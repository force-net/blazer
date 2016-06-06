/*
  Copyright (c) 2013 - 2016 Mark Adler, Robert Vazan, force

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the author be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
  claim that you wrote the original software. If you use this software
  in a product, an acknowledgment in the product documentation would be
  appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
  misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.
*/

#include "stdafx.h"

#ifndef _CRT_SECURE_NO_WARNINGS
#define _CRT_SECURE_NO_WARNINGS
#endif

#define uint32_t unsigned __int32
#define uint8_t unsigned __int8
#define uint64_t unsigned __int64

/*
    Computes CRC-32C using Castagnoli polynomial of 0x82f63b78.
    This polynomial is better at detecting errors than the more common CRC-32 polynomial.
    CRC-32C is implemented in hardware on newer Intel processors.
    This function will use the hardware if available and fall back to fast software implementation.
*/
extern "C" __declspec(dllexport) uint32_t crc32c_append(
    uint32_t crc,               // initial CRC, typically 0, may be used to accumulate CRC from multiple buffers
    const uint8_t *input,       // data to be put through the CRC algorithm
    size_t length);             // length of the data in the input buffer

#define NOMINMAX

#include <algorithm>

typedef const uint8_t *buffer;


#define POLY 0x82f63b78
#define LONG_SHIFT 8192
#define SHORT_SHIFT 256


// include it if you need speed performance instead of library size
// #include "generated-constants.cpp"

static uint32_t table[16][256];

static uint32_t long_shifts[4][256];

static uint32_t short_shifts[4][256];

/* Table-driven software version as a fall-back.  This is about 15 times slower
   than using the hardware instructions.  This assumes little-endian integers,
   as is the case on Intel processors that the assembler code here is for. */
static uint32_t append_table(uint32_t crci, buffer input, size_t length)
{
    buffer next = input;
#ifdef _M_X64
    uint64_t crc;
#else
    uint32_t crc;
#endif

    crc = crci ^ 0xffffffff;
#ifdef _M_X64
    while (length && ((uintptr_t)next & 7) != 0)
    {
        crc = table[0][(crc ^ *next++) & 0xff] ^ (crc >> 8);
        --length;
    }
    while (length >= 16)
    {
        crc ^= *(uint64_t *)next;
        uint64_t high = *(uint64_t *)(next + 8);
        crc = table[15][crc & 0xff]
            ^ table[14][(crc >> 8) & 0xff]
            ^ table[13][(crc >> 16) & 0xff]
            ^ table[12][(crc >> 24) & 0xff]
            ^ table[11][(crc >> 32) & 0xff]
            ^ table[10][(crc >> 40) & 0xff]
            ^ table[9][(crc >> 48) & 0xff]
            ^ table[8][crc >> 56]
            ^ table[7][high & 0xff]
            ^ table[6][(high >> 8) & 0xff]
            ^ table[5][(high >> 16) & 0xff]
            ^ table[4][(high >> 24) & 0xff]
            ^ table[3][(high >> 32) & 0xff]
            ^ table[2][(high >> 40) & 0xff]
            ^ table[1][(high >> 48) & 0xff]
            ^ table[0][high >> 56];
        next += 16;
        length -= 16;
    }
#else
    while (length && ((uintptr_t)next & 3) != 0)
    {
        crc = table[0][(crc ^ *next++) & 0xff] ^ (crc >> 8);
        --length;
    }
    while (length >= 12)
    {
        crc ^= *(uint32_t *)next;
        uint32_t high = *(uint32_t *)(next + 4);
        uint32_t high2 = *(uint32_t *)(next + 8);
        crc = table[11][crc & 0xff]
            ^ table[10][(crc >> 8) & 0xff]
            ^ table[9][(crc >> 16) & 0xff]
            ^ table[8][crc >> 24]
            ^ table[7][high & 0xff]
            ^ table[6][(high >> 8) & 0xff]
            ^ table[5][(high >> 16) & 0xff]
            ^ table[4][high >> 24]
            ^ table[3][high2 & 0xff]
            ^ table[2][(high2 >> 8) & 0xff]
            ^ table[1][(high2 >> 16) & 0xff]
            ^ table[0][high2 >> 24];
        next += 12;
        length -= 12;
    }
#endif
    while (length)
    {
        crc = table[0][(crc ^ *next++) & 0xff] ^ (crc >> 8);
        --length;
    }
    return (uint32_t)crc ^ 0xffffffff;
}

/* Apply the zeros operator table to crc. */
static inline uint32_t shift_crc(uint32_t shift_table[][256], uint32_t crc)
{
    return shift_table[0][crc & 0xff]
        ^ shift_table[1][(crc >> 8) & 0xff]
        ^ shift_table[2][(crc >> 16) & 0xff]
        ^ shift_table[3][crc >> 24];
}

/* Compute CRC-32C using the Intel hardware instruction. */
static uint32_t append_hw(uint32_t crc, buffer buf, size_t len)
{
    buffer next = buf;
    buffer end;
#ifdef _M_X64
    uint64_t crc0, crc1, crc2;      /* need to be 64 bits for crc32q */
#else
    uint32_t crc0, crc1, crc2;
#endif

    /* pre-process the crc */
    crc0 = crc ^ 0xffffffff;

    /* compute the crc for up to seven leading bytes to bring the data pointer
       to an eight-byte boundary */
    while (len && ((uintptr_t)next & 7) != 0)
    {
        crc0 = _mm_crc32_u8(static_cast<uint32_t>(crc0), *next);
        ++next;
        --len;
    }

#ifdef _M_X64
    /* compute the crc on sets of LONG_SHIFT*3 bytes, executing three independent crc
       instructions, each on LONG_SHIFT bytes -- this is optimized for the Nehalem,
       Westmere, Sandy Bridge, and Ivy Bridge architectures, which have a
       throughput of one crc per cycle, but a latency of three cycles */
    while (len >= 3 * LONG_SHIFT)
    {
        crc1 = 0;
        crc2 = 0;
        end = next + LONG_SHIFT;
        do
        {
            crc0 = _mm_crc32_u64(crc0, *reinterpret_cast<const uint64_t *>(next));
            crc1 = _mm_crc32_u64(crc1, *reinterpret_cast<const uint64_t *>(next + LONG_SHIFT));
            crc2 = _mm_crc32_u64(crc2, *reinterpret_cast<const uint64_t *>(next + 2 * LONG_SHIFT));
            next += 8;
        } while (next < end);
        crc0 = shift_crc(long_shifts, static_cast<uint32_t>(crc0)) ^ crc1;
        crc0 = shift_crc(long_shifts, static_cast<uint32_t>(crc0)) ^ crc2;
        next += 2 * LONG_SHIFT;
        len -= 3 * LONG_SHIFT;
    }

    /* do the same thing, but now on SHORT_SHIFT*3 blocks for the remaining data less
       than a LONG_SHIFT*3 block */
    while (len >= 3 * SHORT_SHIFT)
    {
        crc1 = 0;
        crc2 = 0;
        end = next + SHORT_SHIFT;
        do
        {
            crc0 = _mm_crc32_u64(crc0, *reinterpret_cast<const uint64_t *>(next));
            crc1 = _mm_crc32_u64(crc1, *reinterpret_cast<const uint64_t *>(next + SHORT_SHIFT));
            crc2 = _mm_crc32_u64(crc2, *reinterpret_cast<const uint64_t *>(next + 2 * SHORT_SHIFT));
            next += 8;
        } while (next < end);
        crc0 = shift_crc(short_shifts, static_cast<uint32_t>(crc0)) ^ crc1;
        crc0 = shift_crc(short_shifts, static_cast<uint32_t>(crc0)) ^ crc2;
        next += 2 * SHORT_SHIFT;
        len -= 3 * SHORT_SHIFT;
    }

    /* compute the crc on the remaining eight-byte units less than a SHORT_SHIFT*3
    block */
    end = next + (len - (len & 7));
    while (next < end)
    {
        crc0 = _mm_crc32_u64(crc0, *reinterpret_cast<const uint64_t *>(next));
        next += 8;
    }
#else
    /* compute the crc on sets of LONG_SHIFT*3 bytes, executing three independent crc
    instructions, each on LONG_SHIFT bytes -- this is optimized for the Nehalem,
    Westmere, Sandy Bridge, and Ivy Bridge architectures, which have a
    throughput of one crc per cycle, but a latency of three cycles */
    while (len >= 3 * LONG_SHIFT)
    {
        crc1 = 0;
        crc2 = 0;
        end = next + LONG_SHIFT;
        do
        {
            crc0 = _mm_crc32_u32(crc0, *reinterpret_cast<const uint32_t *>(next));
            crc1 = _mm_crc32_u32(crc1, *reinterpret_cast<const uint32_t *>(next + LONG_SHIFT));
            crc2 = _mm_crc32_u32(crc2, *reinterpret_cast<const uint32_t *>(next + 2 * LONG_SHIFT));
            next += 4;
        } while (next < end);
        crc0 = shift_crc(long_shifts, static_cast<uint32_t>(crc0)) ^ crc1;
        crc0 = shift_crc(long_shifts, static_cast<uint32_t>(crc0)) ^ crc2;
        next += 2 * LONG_SHIFT;
        len -= 3 * LONG_SHIFT;
    }

    /* do the same thing, but now on SHORT_SHIFT*3 blocks for the remaining data less
    than a LONG_SHIFT*3 block */
    while (len >= 3 * SHORT_SHIFT)
    {
        crc1 = 0;
        crc2 = 0;
        end = next + SHORT_SHIFT;
        do
        {
            crc0 = _mm_crc32_u32(crc0, *reinterpret_cast<const uint32_t *>(next));
            crc1 = _mm_crc32_u32(crc1, *reinterpret_cast<const uint32_t *>(next + SHORT_SHIFT));
            crc2 = _mm_crc32_u32(crc2, *reinterpret_cast<const uint32_t *>(next + 2 * SHORT_SHIFT));
            next += 4;
        } while (next < end);
        crc0 = shift_crc(short_shifts, static_cast<uint32_t>(crc0)) ^ crc1;
        crc0 = shift_crc(short_shifts, static_cast<uint32_t>(crc0)) ^ crc2;
        next += 2 * SHORT_SHIFT;
        len -= 3 * SHORT_SHIFT;
    }

    /* compute the crc on the remaining eight-byte units less than a SHORT_SHIFT*3
    block */
    end = next + (len - (len & 7));
    while (next < end)
    {
        crc0 = _mm_crc32_u32(crc0, *reinterpret_cast<const uint32_t *>(next));
        next += 4;
    }
#endif
    len &= 7;

    /* compute the crc for up to seven trailing bytes */
    while (len)
    {
        crc0 = _mm_crc32_u8(static_cast<uint32_t>(crc0), *next);
        ++next;
        --len;
    }

    /* return a post-processed crc */
    return static_cast<uint32_t>(crc0) ^ 0xffffffff;
}

static bool detect_hw()
{
    int info[4];
    __cpuid(info, 1);
    return (info[2] & (1 << 20)) != 0;
}

uint32_t (*append_func)(uint32_t, buffer, size_t);

// static bool hw_available = detect_hw();

void calculate_table() {
	for(int i = 0; i < 256; i++) {
		uint32_t res = (uint32_t)i;
		for(int t = 0; t < 16; t++) {
			for (int k = 0; k < 8; k++) res = (res & 1) == 1 ? POLY ^ (res >> 1) : (res >> 1);
			table[t][i] = res;
		}
	}
}

void calculate_hw() {
	for(int i = 0; i < 256; i++) {
		uint32_t res = (uint32_t)i;
		for (int k = 0; k < 8 * (SHORT_SHIFT - 4); k++) res = (res & 1) == 1 ? POLY ^ (res >> 1) : (res >> 1);
		for(int t = 0; t < 4; t++) {
			for (int k = 0; k < 8; k++) res = (res & 1) == 1 ? POLY ^ (res >> 1) : (res >> 1);
			short_shifts[3 - t][i] = res;
		}
		for (int k = 0; k < 8 * (LONG_SHIFT - 4 - SHORT_SHIFT); k++) res = (res & 1) == 1 ? POLY ^ (res >> 1) : (res >> 1);
		for(int t = 0; t < 4; t++) {
			for (int k = 0; k < 8; k++) res = (res & 1) == 1 ? POLY ^ (res >> 1) : (res >> 1);
			long_shifts[3 - t][i] = res;
		}
	}
}

void _crc32c_init()
{
	if (append_func == NULL)
	{
		if (detect_hw()) {
			append_func = append_hw;
			calculate_hw();
		} else {
			calculate_table();
			append_func = append_table;
		}
	}
}


extern "C" __declspec(dllexport) uint32_t crc32c_append(uint32_t crc, buffer input, size_t length)
{
	return append_func(crc, input, length);
}

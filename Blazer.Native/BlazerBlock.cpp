#include "stdafx.h"
#include <windows.h>

// #define Mul 0x736AE249u
#define Mul  1527631329
#define HASH_TABLE_BITS 16

#define HASH_TABLE_BITS 16
#define HASH_TABLE_LEN ((1 << HASH_TABLE_BITS) - 1)

#define MIN_SEQ_LEN 4

#define MIN(a, b) ((a) <= (b) ? (a) : (b))

static inline unsigned char* copy_memory(unsigned char* src, unsigned char* dst, __int32 count)
{
	/*while (count > 0 && ((int)src & 7) != 0)
	{
		*dst++ = *src++;
		count--;
	}*/

	while (count >= sizeof(int))
	{
		*(int*)dst = *(int*)src;
		dst += sizeof(int);
		src += sizeof(int);
		count -= sizeof(int);
	}
	
	while (count-- > 0)
	{
		*dst++ = *src++;
	}

	return dst;
}

extern "C" __declspec(dllexport) __int32 blazer_block_compress_block(unsigned char* bufferIn, __int32 bufferInOffset, __int32 bufferInLength, unsigned char* bufferOut, __int32 bufferOutOffset)
{
	HANDLE hHeap = GetProcessHeap();
	// int hashArr[HASH_TABLE_LEN + 1];
	__int32* hashArr = (__int32*)HeapAlloc(hHeap, HEAP_ZERO_MEMORY, sizeof(__int32) * (HASH_TABLE_LEN + 1));
	int idxIn = bufferInOffset;
	int lastProcessedIdxIn = idxIn;
	int idxOut = bufferOutOffset;

	int cntLit;

	int iterMax = bufferInLength - 4;

	unsigned char* bufferOutOrig = bufferOut;
	bufferOut += bufferOutOffset;

	unsigned __int32 mulEl = 0;

	if (bufferInLength > 3)
		mulEl = (unsigned __int32)(bufferIn[0] << 16 | bufferIn[1] << 8 | bufferIn[2]);

	while (idxIn < iterMax)
	{
		int idxInP3 = idxIn + 3;
		unsigned char elemP0 = bufferIn[idxInP3];

		mulEl = (mulEl << 8) | elemP0;
		unsigned int hashKey = (mulEl  * Mul) >> (32 - HASH_TABLE_BITS);
		int hashVal = hashArr[hashKey];
		hashArr[hashKey] = idxInP3;

		int backRef = idxInP3 - hashVal;
		if (hashVal > 0 && hashKey != 0xffff && ((backRef < 257 || bufferIn[hashVal + 1] == bufferIn[idxIn + 4])
				&& mulEl == (unsigned __int32)((bufferIn[hashVal - 3] << 24) | (bufferIn[hashVal - 2] << 16) | (bufferIn[hashVal - 1] << 8) | bufferIn[hashVal])))
		{
			int origIdxIn = idxIn;
			hashVal += 4 - 3;
			idxIn += 4;

			while (idxIn < bufferInLength)
			{
				elemP0 = bufferIn[idxIn];
				mulEl = (mulEl << 8) | elemP0;
				hashArr[(mulEl * Mul) >> (32 - HASH_TABLE_BITS)] = idxIn;

				if (bufferIn[hashVal++] == elemP0)
				{
					// hashVal++;
					idxIn++;
				}
				else break;
			}

			if (idxIn < iterMax)
			{
				mulEl = (mulEl << 8) | bufferIn[idxIn + 1];
				hashArr[(mulEl * Mul) >> (32 - HASH_TABLE_BITS)] = idxIn + 1;
				mulEl = (mulEl << 8) | bufferIn[idxIn + 2];
				hashArr[(mulEl * Mul) >> (32 - HASH_TABLE_BITS)] = idxIn + 2;
			}

			int seqLen;
			cntLit = origIdxIn - lastProcessedIdxIn;

			if (backRef >= 256 + 1)
			{
				seqLen = idxIn - origIdxIn - MIN_SEQ_LEN/* - 1*/;
				*(bufferOut++) = (unsigned char)(((MIN(cntLit, 7) << 4) | MIN(seqLen, 15)) | 128);

				*((unsigned __int16*)bufferOut) = hashKey;
				bufferOut += 2;
			}
			else
			{
				seqLen = idxIn - origIdxIn - MIN_SEQ_LEN;
				*(bufferOut++) = (unsigned char)((MIN(cntLit, 7) << 4) | MIN(seqLen, 15));

				// 1 is always min, should not write it
				*(bufferOut++) = (unsigned char)(backRef - 1);
			}


			if (cntLit >= 7)
			{
				__int32 c = cntLit - 7;
				if (c < 253) *(bufferOut++) = (unsigned char)c;
				else if (c < 253 + 256)
				{
					*(bufferOut++) = 253;
					*(bufferOut++) = (unsigned char)(c - 253);
				}
				else if (c < 253 + (256 * 256))
				{
					*(bufferOut++) = 254;
					c -= 253 + 256;
					*((unsigned __int16*)bufferOut) = c;
					bufferOut += 2;
				}
				else
				{
					*(bufferOut++) = 255;
					c -= 253 + (256 * 256);
					*((unsigned __int32*)bufferOut) = c;
					bufferOut += 4;
				}
			}

			if (seqLen >= 15)
			{
				__int32 c = seqLen - 15;
				if (c < 253) *(bufferOut++) = (unsigned char)c;
				else if (c < 253 + 256)
				{
					*(bufferOut++) = 253;
					*(bufferOut++) = (unsigned char)(c - 253);
				}
				else if (c < 253 + (256 * 256))
				{
					*(bufferOut++) = 254;
					c -= 253 + 256;
					*((unsigned __int16*)bufferOut) = c;
					bufferOut += 2;
				}
				else
				{
					*(bufferOut++) = 255;
					c -= 253 + (256 * 256);
					*((unsigned __int32*)bufferOut) = c;
					bufferOut += 4;
				}
			}

			bufferOut = copy_memory(bufferIn + origIdxIn - cntLit, bufferOut, cntLit);
			
			lastProcessedIdxIn = idxIn;
			continue;
		}

		idxIn++;
	}

	cntLit = bufferInLength - lastProcessedIdxIn;
	idxIn = bufferInLength;

	if (cntLit > 0)
	{
		*(bufferOut++) = (unsigned char)(MIN(127, cntLit) + 128);
		*((unsigned __int16*)bufferOut) = 0xffff;
		bufferOut += 2;

		if (cntLit >= 127)
		{
			__int32 c = cntLit - 127;
			if (c < 253) *(bufferOut++) = (unsigned char)c;
			else if (c < 253 + 256)
			{
				*(bufferOut++) = 253;
				*(bufferOut++) = (unsigned char)(c - 253);
			}
			else if (c < 253 + (256 * 256))
			{
				*(bufferOut++) = 254;
				c -= 253 + 256;
				*((unsigned __int16*)bufferOut) = c;
				bufferOut += 2;
			}
			else
			{
				*(bufferOut++) = 255;
				c -= 253 + (256 * 256);
				*((unsigned __int32*)bufferOut) = c;
				bufferOut += 4;
			}
		}

		while (cntLit > 0)
		{
			*(bufferOut++) = bufferIn[idxIn - cntLit];
			cntLit--;
		}
	}

	HeapFree(hHeap, 0, hashArr);
	return (__int32)(bufferOut - bufferOutOrig);
}

extern "C" __declspec(dllexport) __int32 blazer_block_decompress_block(unsigned char* bufferIn, __int32 bufferInOffset, __int32 bufferInLength, unsigned char* bufferOut, __int32 bufferOutOffset, __int32 bufferOutLength)
{
	HANDLE hHeap = GetProcessHeap();
	// int hashArr[HASH_TABLE_LEN + 1];
	__int32* hashArr = (__int32*)HeapAlloc(hHeap, HEAP_ZERO_MEMORY, sizeof(__int32) * (HASH_TABLE_LEN + 1));

	unsigned char* bufferInEnd = bufferIn + bufferInLength;
	// __int32 idxIn = bufferInOffset;
	bufferIn += bufferInOffset;
	int idxOut = bufferOutOffset;
	unsigned __int32 mulEl = 0;

	while (bufferIn < bufferInEnd)
	{
		unsigned char elem = *(bufferIn++);

		int seqCntFirst = elem & 0xf;
		int litCntFirst = (elem >> 4) & 7;

		int litCnt = litCntFirst;
		int seqCnt;
		int backRef;
		int hashIdx = -1;

		if (elem >= 128)
		{
			hashIdx = *(unsigned __int16*)(bufferIn);
			seqCnt = seqCntFirst + /*5*/4;
			bufferIn += 2;
			if (hashIdx == 0xffff)
			{
				seqCnt = 0;
				seqCntFirst = 0;
				litCnt = elem - 128;
				litCntFirst = litCnt == 127 ? 7 : 0;
			}
		}
		else
		{
			backRef = *(bufferIn++) + 1;
			seqCnt = seqCntFirst + 4;
		}

		if (litCntFirst == 7)
		{
			unsigned char litCntR = *(bufferIn++);
			
			if (litCntR < 253) litCnt += litCntR;
			else if (litCntR == 253)
				litCnt += 253 + *(bufferIn++);
			else if (litCntR == 254)
			{
				litCnt += 253 + 256 + *(unsigned __int16*)(bufferIn);
				bufferIn += 2;
			}
			else
			{
				litCnt += 253 + (256 * 256) + *(unsigned __int32*)(bufferIn);
				bufferIn += 4;
			}
		}

		if (seqCntFirst == 15)
		{
			unsigned char seqCntR = *(bufferIn++);
			if (seqCntR < 253) seqCnt += seqCntR;
			else if (seqCntR == 253)
				seqCnt += 253 + *(bufferIn++);
			else if (seqCntR == 254)
			{
				seqCnt += 253 + 256 + *(unsigned __int16*)(bufferIn);
				bufferIn += 2;
			}
			else
			{
				seqCnt += 253 + (256 * 256) + *(unsigned __int32*)(bufferIn);
				bufferIn += 4;
			}
		}

		// TODO: check upper limit
		int maxOutLength = idxOut + litCnt + seqCnt;
		if (maxOutLength > bufferOutLength)
		{
			return -1;
		}

		while (--litCnt >= 0)
		{
			unsigned char v = *(bufferIn++);
			mulEl = (mulEl << 8) | v;
			hashArr[(mulEl * Mul) >> (32 - HASH_TABLE_BITS)] = idxOut;
			bufferOut[idxOut++] = v;
		}

		int inRepIdx = hashIdx >= 0 ? hashArr[hashIdx] - 3 : idxOut - backRef;
		while (--seqCnt >= 0)
		{
			unsigned char v = bufferOut[inRepIdx++];
			mulEl = (mulEl << 8) | v;
			hashArr[(mulEl * Mul) >> (32 - HASH_TABLE_BITS)] = idxOut;
			bufferOut[idxOut++] = v;
		}
	}

	HeapFree(hHeap, 0, hashArr);
	return idxOut;
}

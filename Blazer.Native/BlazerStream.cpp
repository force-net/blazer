#include "stdafx.h"

#define HASH_TABLE_BITS  16
#define HASH_TABLE_LEN  ((1 << HASH_TABLE_BITS) - 1)
#define MAX_BACK_REF  ((1 << 16) + 256)
#define MIN_SEQ_LEN  4
// #define MUL  0x0C5AE896A

// carefully selected random number
#define MUL 1527631329

#define MIN(a, b) ((a) < (b) ? (a) : (b))

inline unsigned char* copy_memory(unsigned char* src, unsigned char* dst, __int32 count)
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

extern "C" __declspec(dllexport) __int32 blazer_stream_compress_block(unsigned char* bufferIn, __int32 bufferInOffset, __int32 bufferInLength, __int32 bufferInShift, unsigned char* bufferOut, __int32 bufferOutOffset, __int32* hashArr)
{
	int cntLit;

	unsigned __int32 mulEl = 0;

	unsigned char* bufferOutOrig = bufferOut;
	bufferOut += bufferOutOffset;

	int iterMax = bufferInLength - 1;

	int idxIn = bufferInOffset;
	int lastProcessedIdxIn = idxIn + 3;
	int globalOfs = bufferInShift;
	if (bufferInLength - idxIn > 3)
	{
		mulEl = (unsigned __int32)(bufferIn[idxIn] << 16 | bufferIn[idxIn+1] << 8 | bufferIn[idxIn+2]);
		idxIn += 3;
	}
	else
	{
		idxIn = bufferInLength;
	}

	while (idxIn < iterMax)
	{
		unsigned char elemP0 = bufferIn[idxIn];

		mulEl = (mulEl << 8) | elemP0;
		unsigned __int32 hashKey = (mulEl * MUL) >> (32 - HASH_TABLE_BITS);
		int hashVal = hashArr[hashKey] - globalOfs;
		hashArr[hashKey] = idxIn + globalOfs;
		int backRef = idxIn - hashVal;
		int isBig = backRef < 257 ? 0 : 1;
		if (hashVal > 0
			&& backRef < MAX_BACK_REF
			&& ((!isBig || bufferIn[hashVal + 1] == bufferIn[idxIn + 1])
				&& mulEl == (unsigned __int32)((bufferIn[hashVal - 3] << 24) | (bufferIn[hashVal - 2] << 16) | (bufferIn[hashVal - 1] << 8) | bufferIn[hashVal - 0])))
		{
			cntLit = idxIn - lastProcessedIdxIn;

			hashVal++;
			idxIn++;

			while (idxIn < bufferInLength)
			{
				elemP0 = bufferIn[idxIn];
				mulEl = (mulEl << 8) | elemP0;
				hashKey = (mulEl * MUL) >> (32 - HASH_TABLE_BITS);
				hashArr[hashKey] = idxIn + globalOfs;

				if (bufferIn[hashVal] == elemP0)
				{
					hashVal++;
					idxIn++;
				}
				else break;
			}

			int seqLen = idxIn - cntLit - lastProcessedIdxIn - MIN_SEQ_LEN + 3/* - isBig*/;

			if (backRef >= 256 + 1)
			{
				backRef -= 256 + 1;
				*(bufferOut++) = (unsigned char)(((MIN(cntLit, 7) << 4) | MIN(seqLen, 15)) + 128);

				*((unsigned __int16*)bufferOut) = backRef;
				bufferOut += 2;
			}
			else
			{
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

			bufferOut = copy_memory(bufferIn + lastProcessedIdxIn - 3, bufferOut, cntLit);

			idxIn += 3;
			lastProcessedIdxIn = idxIn;

			if (idxIn < bufferInLength)
			{
				mulEl = (mulEl << 8) | bufferIn[idxIn - 2];
				hashKey = (mulEl * MUL) >> (32 - HASH_TABLE_BITS);
				hashArr[hashKey] = idxIn - 2 + globalOfs;

				mulEl = (mulEl << 8) | bufferIn[idxIn - 1];
				hashKey = (mulEl * MUL) >> (32 - HASH_TABLE_BITS);
				hashArr[hashKey] = idxIn - 1 + globalOfs;
			}

			continue;
		}

		idxIn++;
	}

	cntLit = bufferInLength - lastProcessedIdxIn + 3;
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

	return (__int32)(bufferOut - bufferOutOrig);
}

extern "C" __declspec(dllexport) __int32 blazer_stream_decompress_block(unsigned char* bufferIn, __int32 bufferInOffset, __int32 bufferInLength, unsigned char* bufferOut, __int32 bufferOutOffset, __int32 bufferOutLength)
{
	bufferIn += bufferInOffset;
	unsigned char* bufferInEnd = bufferIn + bufferInLength;

	unsigned char* bufferOutOrig = bufferOut;
	unsigned char* bufferOutEnd = bufferOut + bufferOutLength;
	bufferOut += bufferOutOffset;

	while (bufferIn < bufferInEnd)
	{
		unsigned char elem = *(bufferIn++);

		int seqCntFirst = elem & 0xf;
		int litCntFirst = (elem >> 4) & 7;

		int litCnt = litCntFirst;
		int seqCnt;
		int backRef;

		if (elem >= 128)
		{
			backRef = *(unsigned __int16*)(bufferIn) + 257;
			seqCnt = seqCntFirst + /*5*/ 4;
			bufferIn += 2;
			if (backRef == 0xffff + 257)
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

		unsigned char* maxOutLength = bufferOut + litCnt + seqCnt;
		if (maxOutLength >= bufferOutEnd)
		{
			return -1;
			// throw new IndexOutOfRangeException("Invalid stream structure");
		}

		bufferOut = copy_memory(bufferIn, bufferOut, litCnt);
		bufferIn += litCnt;

		if (backRef >= seqCnt && seqCnt > 8)
		{
			bufferOut = copy_memory(bufferOut - backRef, bufferOut, seqCnt);
		}
		else
		{
			while (--seqCnt >= 0)
			{
				*(bufferOut) = *(bufferOut - backRef);
				bufferOut++;
			}
		}
	}

	return (__int32)(bufferOut - bufferOutOrig);
}

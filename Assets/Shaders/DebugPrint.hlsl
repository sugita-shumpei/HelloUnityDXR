#ifndef DEBUG_PRINT_HLSL
#define DEBUG_PRINT_HLSL

#define DEBUG_PRINT_BUFFER_CAPACITY     64
#define DEBUG_PRINT_ERROR_CODE_OK       0
#define DEBUG_PRINT_ERROR_CODE_OVERFLOW 1

RWStructuredBuffer<uint> debugPrintBuffer       : register(u6);
RWStructuredBuffer<uint> debugPrintCounterBuffer: register(u7);
static uint debugPrintStorage[DEBUG_PRINT_BUFFER_CAPACITY];
static uint debugPrintStorageCount = 0;
static uint debugPrintErrorCode    = DEBUG_PRINT_ERROR_CODE_OK;

uint getPrintErrorCode() {
	return debugPrintErrorCode;
}
void flushPrintStream() {
    uint debugPrintBufferStride   = 0;
    uint debugPrintBufferCapacity = 0;
    debugPrintBuffer.GetDimensions(debugPrintBufferCapacity, debugPrintBufferStride);
    uint debugPrintBufferIndex    = 0;
	uint debugPrintStorageCount4  = (debugPrintStorageCount + 3) / 4;
    InterlockedAdd(debugPrintCounterBuffer[0], debugPrintStorageCount4, debugPrintBufferIndex);
    if (debugPrintBufferIndex > debugPrintBufferCapacity) {
        debugPrintErrorCode = DEBUG_PRINT_ERROR_CODE_OVERFLOW;
        return;
    }
    debugPrintStorageCount4 = min(min(debugPrintStorageCount4, debugPrintBufferCapacity - debugPrintBufferIndex), DEBUG_PRINT_BUFFER_CAPACITY);
	if (debugPrintStorageCount4 == 0) {
        debugPrintErrorCode = DEBUG_PRINT_ERROR_CODE_OVERFLOW;
		return;
	}
    [unroll(DEBUG_PRINT_BUFFER_CAPACITY)]
    for (uint i = 0; i < debugPrintStorageCount4; i++) {
        debugPrintBuffer[debugPrintBufferIndex + i] = debugPrintStorage[i];
    }
	for (uint i = 0; i < debugPrintStorageCount4; i++) {
		debugPrintStorage[i] = 0;
	}
    debugPrintStorageCount = 0;
}
void writeChar(uint ch) {
    if (debugPrintStorageCount >= DEBUG_PRINT_BUFFER_CAPACITY * 4) {
        return;
    }
    uint debugPrintStorageIdx = debugPrintStorageCount / 4;
    uint debugPrintStorageOff = debugPrintStorageCount % 4;
    debugPrintStorage[debugPrintStorageIdx] |= (ch << (debugPrintStorageOff * 8));
    debugPrintStorageCount++;
}
void debugPrintEndl() {
	writeChar(0x0A);
}
void debugPrint(uint ch0) {
    writeChar(ch0);
}
void debugPrint(uint ch0, uint ch1) {
    writeChar(ch0);
    writeChar(ch1);
}
void debugPrint(uint ch0, uint ch1, uint ch2) {
    writeChar(ch0);
    writeChar(ch1);
    writeChar(ch2);
}
void debugPrint(uint ch0, uint ch1, uint ch2, uint ch3) {
    writeChar(ch0);
    writeChar(ch1);
    writeChar(ch2);
    writeChar(ch3);
}
void debugPrint(uint ch0, uint ch1, uint ch2, uint ch3, uint ch4) {
    writeChar(ch0);
    writeChar(ch1);
    writeChar(ch2);
    writeChar(ch3);
    writeChar(ch4);
}
void debugPrint(uint ch0, uint ch1, uint ch2, uint ch3, uint ch4, uint ch5) {
    writeChar(ch0);
    writeChar(ch1);
    writeChar(ch2);
    writeChar(ch3);
    writeChar(ch4);
    writeChar(ch5);
}
void debugPrint(uint ch0, uint ch1, uint ch2, uint ch3, uint ch4, uint ch5, uint ch6) {
    writeChar(ch0);
    writeChar(ch1);
    writeChar(ch2);
    writeChar(ch3);
    writeChar(ch4);
    writeChar(ch5);
    writeChar(ch6);
}
void debugPrint(uint ch0, uint ch1, uint ch2, uint ch3, uint ch4, uint ch5, uint ch6, uint ch7) {
    writeChar(ch0);
    writeChar(ch1);
    writeChar(ch2);
    writeChar(ch3);
    writeChar(ch4);
    writeChar(ch5);
    writeChar(ch6);
    writeChar(ch7);
}
void debugPrint(uint ch0, uint ch1, uint ch2, uint ch3, uint ch4, uint ch5, uint ch6, uint ch7, uint ch8) {
	writeChar(ch0);
	writeChar(ch1);
	writeChar(ch2);
	writeChar(ch3);
	writeChar(ch4);
	writeChar(ch5);
	writeChar(ch6);
	writeChar(ch7);
	writeChar(ch8);
}
void debugPrint(uint ch0, uint ch1, uint ch2, uint ch3, uint ch4, uint ch5, uint ch6, uint ch7, uint ch8, uint ch9) {
	writeChar(ch0);
	writeChar(ch1);
	writeChar(ch2);
	writeChar(ch3);
	writeChar(ch4);
	writeChar(ch5);
	writeChar(ch6);
	writeChar(ch7);
	writeChar(ch8);
	writeChar(ch9);
}
void debugPrint(uint ch0, uint ch1, uint ch2, uint ch3, uint ch4, uint ch5, uint ch6, uint ch7, uint ch8, uint ch9, uint ch10) {
	writeChar(ch0);
	writeChar(ch1);
	writeChar(ch2);
	writeChar(ch3);
	writeChar(ch4);
	writeChar(ch5);
	writeChar(ch6);
	writeChar(ch7);
	writeChar(ch8);
	writeChar(ch9);
	writeChar(ch10);
}
void debugPrint(uint ch0, uint ch1, uint ch2, uint ch3, uint ch4, uint ch5, uint ch6, uint ch7, uint ch8, uint ch9, uint ch10, uint ch11) {
	writeChar(ch0);
	writeChar(ch1);
	writeChar(ch2);
	writeChar(ch3);
	writeChar(ch4);
	writeChar(ch5);
	writeChar(ch6);
	writeChar(ch7);
	writeChar(ch8);
	writeChar(ch9);
	writeChar(ch10);
	writeChar(ch11);
}
void debugPrint(uint ch0, uint ch1, uint ch2, uint ch3, uint ch4, uint ch5, uint ch6, uint ch7, uint ch8, uint ch9, uint ch10, uint ch11, uint ch12) {
	writeChar(ch0);
	writeChar(ch1);
	writeChar(ch2);
	writeChar(ch3);
	writeChar(ch4);
	writeChar(ch5);
	writeChar(ch6);
	writeChar(ch7);
	writeChar(ch8);
	writeChar(ch9);
	writeChar(ch10);
	writeChar(ch11);
	writeChar(ch12);
}
void debugPrint(uint ch0, uint ch1, uint ch2, uint ch3, uint ch4, uint ch5, uint ch6, uint ch7, uint ch8, uint ch9, uint ch10, uint ch11, uint ch12, uint ch13) {
	writeChar(ch0);
	writeChar(ch1);
	writeChar(ch2);
	writeChar(ch3);
	writeChar(ch4);
	writeChar(ch5);
	writeChar(ch6);
	writeChar(ch7);
	writeChar(ch8);
	writeChar(ch9);
	writeChar(ch10);
	writeChar(ch11);
	writeChar(ch12);
	writeChar(ch13);
}
void debugPrint(uint ch0, uint ch1, uint ch2, uint ch3, uint ch4, uint ch5, uint ch6, uint ch7, uint ch8, uint ch9, uint ch10, uint ch11, uint ch12, uint ch13, uint ch14) {
	writeChar(ch0);
	writeChar(ch1);
	writeChar(ch2);
	writeChar(ch3);
	writeChar(ch4);
	writeChar(ch5);
	writeChar(ch6);
	writeChar(ch7);
	writeChar(ch8);
	writeChar(ch9);
	writeChar(ch10);
	writeChar(ch11);
	writeChar(ch12);
	writeChar(ch13);
	writeChar(ch14);
}
void debugPrint(uint ch0, uint ch1, uint ch2, uint ch3, uint ch4, uint ch5, uint ch6, uint ch7, uint ch8, uint ch9, uint ch10, uint ch11, uint ch12, uint ch13, uint ch14, uint ch15) {
	writeChar(ch0);
	writeChar(ch1);
	writeChar(ch2);
	writeChar(ch3);
	writeChar(ch4);
	writeChar(ch5);
	writeChar(ch6);
	writeChar(ch7);
	writeChar(ch8);
	writeChar(ch9);
	writeChar(ch10);
	writeChar(ch11);
	writeChar(ch12);
	writeChar(ch13);
	writeChar(ch14);
	writeChar(ch15);
}
void debugPrint(uint ch0, uint ch1, uint ch2, uint ch3, uint ch4, uint ch5, uint ch6, uint ch7, uint ch8, uint ch9, uint ch10, uint ch11, uint ch12, uint ch13, uint ch14, uint ch15, uint ch16) {
	writeChar(ch0);
	writeChar(ch1);
	writeChar(ch2);
	writeChar(ch3);
	writeChar(ch4);
	writeChar(ch5);
	writeChar(ch6);
	writeChar(ch7);
	writeChar(ch8);
	writeChar(ch9);
	writeChar(ch10);
	writeChar(ch11);
	writeChar(ch12);
	writeChar(ch13);
	writeChar(ch14);
	writeChar(ch15);
	writeChar(ch16);
}
void debugPrint(uint ch0, uint ch1, uint ch2, uint ch3, uint ch4, uint ch5, uint ch6, uint ch7, uint ch8, uint ch9, uint ch10, uint ch11, uint ch12, uint ch13, uint ch14, uint ch15, uint ch16, uint ch17) {
	writeChar(ch0);
	writeChar(ch1);
	writeChar(ch2);
	writeChar(ch3);
	writeChar(ch4);
	writeChar(ch5);
	writeChar(ch6);
	writeChar(ch7);
	writeChar(ch8);
	writeChar(ch9);
	writeChar(ch10);
	writeChar(ch11);
	writeChar(ch12);
	writeChar(ch13);
	writeChar(ch14);
	writeChar(ch15);
	writeChar(ch16);
	writeChar(ch17);
}
void debugPrint1u(uint v)
{
    if (v == 0) {
        writeChar('0');
        return;
    }
    // 上のコードでは逆に表示されてしまう
    uint digit = log10(v);
    for (uint i = 0; i <= digit; i++) {
        const uint exp10 = pow(10, digit - i);
        uint d = v / exp10;
        writeChar('0' + d);
        v -= d * exp10;
    }
}
void debugPrint2u(uint2 v) {
    debugPrint1u(v.x); writeChar(','); debugPrint1u(v.y);
}
void debugPrint2u(uint  v1, uint v2) {
    debugPrint1u(v1); writeChar(','); debugPrint1u(v2);
}
void debugPrint3u(uint3 v) {
    debugPrint1u(v.x); writeChar(','); debugPrint1u(v.y); writeChar(','); debugPrint1u(v.z);
}
void debugPrint3u(uint  v1, uint v2, uint v3) {
    debugPrint1u(v1); writeChar(','); debugPrint1u(v2); writeChar(','); debugPrint1u(v3);
}
void debugPrint4u(uint4 v) {
    debugPrint1u(v.x); writeChar(','); debugPrint1u(v.y); writeChar(','); debugPrint1u(v.z); writeChar(','); debugPrint1u(v.w);
}
void debugPrint4u(uint  v1, uint v2, uint v3, uint v4) {
    debugPrint1u(v1); writeChar(','); debugPrint1u(v2); writeChar(','); debugPrint1u(v3); writeChar(','); debugPrint1u(v4);
}
void debugPrint1i(int v) {
    if (v < 0) {
        writeChar('-'); v = -v;
    }
    debugPrint1u((uint)v);
}
void debugPrint2i(int2 v) {
    debugPrint1i(v.x); writeChar(','); debugPrint1i(v.y);
}
void debugPrint2i(int  v1, int v2) {
    debugPrint1i(v1); writeChar(','); debugPrint1i(v2);
}
void debugPrint3i(int3 v) {
    debugPrint1i(v.x); writeChar(','); debugPrint1i(v.y); writeChar(','); debugPrint1i(v.z);
}
void debugPrint3i(int  v1, int v2, int v3) {
    debugPrint1i(v1); writeChar(','); debugPrint1i(v2); writeChar(','); debugPrint1i(v3);
}
void debugPrint4i(int4 v) {
    debugPrint1i(v.x); writeChar(','); debugPrint1i(v.y); writeChar(','); debugPrint1i(v.z); writeChar(','); debugPrint1i(v.w);
}
void debugPrint4i(int  v1, int v2, int v3, int v4) {
    debugPrint1i(v1); writeChar(','); debugPrint1i(v2); writeChar(','); debugPrint1i(v3); writeChar(','); debugPrint1i(v4);
}
void debugPrint1uWithPad5digits(uint v) {
    if (v == 0) {
        writeChar('0');
        return;
    }
	// v = 1  -> 00001
	// v = 12 -> 00012
	for (uint i = 0; i <= 4; i++) {
		const uint exp10 = pow(10, 4 - i);
		uint d = (v / exp10)%10;
		writeChar('0' + d);
		v -= d * exp10;
	}
}
void debugPrint1uWithPad7digits(uint v) {
    if (v == 0) {
        writeChar('0');
        return;
    }
    // v = 1  -> 0000001
    // v = 12 -> 0000012
	for (uint i = 0; i <= 6; i++) {
		const uint exp10 = pow(10, 6 - i);
        uint d = (v / exp10) % 10;
		writeChar('0' + d);
		v -= d * exp10;
	}
}
void debugPrint1f(float v) {
    if (v == 0) {
        writeChar('0');
        return;
    }
	if ((asuint(v) & 0x7fffffff) > 0x7f800000) {
		writeChar('N');
		writeChar('a');
		writeChar('N');
		return;
    }
    if (v < 0) {
        writeChar('-');
        v = -v;
    }
    if (isinf(v)) {
        writeChar('I');
        writeChar('n');
        writeChar('f');
        return;
    }

	if (v > 1e+6) {
		// 1e+6以上の場合は指数表記
		float logF        = log10(v);
		int   expPart     = ((int)logF);
		int   exp10       = pow(10, expPart);
		float fracPartF   = v / exp10;
        uint intPart      = (uint)fracPartF;
        float deciPartF   = (fracPartF - intPart);
        uint  deciPartI   = (uint)(deciPartF * 100000);
        debugPrint1u(intPart);
        writeChar('.');
        debugPrint1uWithPad5digits(deciPartI);
		writeChar('E');
		debugPrint1i(expPart);
		return;
	}
    else if (v > 0.0001) {
        uint intPart = (uint)v;
        float deciPartF = (v - intPart);
        uint  deciPartI = (uint)(deciPartF * 10000000);
        debugPrint1u(intPart);
        writeChar('.');
        debugPrint1uWithPad7digits(deciPartI);
    }else {
		// 0.001未満の場合は対数を取って指数表記
		// 基本的に対数は負の値になる
		float logF        = log10(v);
        int   expPart     = ((int)logF) - 1;
		float logFracPart = -(float)expPart + logF;
        int   exp10       = pow(10,-expPart);
        float fracPartF   = v * exp10;
        uint intPart    = (uint)fracPartF;
        float deciPartF = (fracPartF - intPart);
        uint  deciPartI = (uint)(deciPartF * 100000);
        debugPrint1u(intPart);
        writeChar('.');
        debugPrint1uWithPad5digits(deciPartI);
        writeChar('E');
        debugPrint1i(expPart);
	}
}
void debugPrint2f(float2 v) {
    debugPrint1f(v.x); writeChar(','); debugPrint1f(v.y);
}
void debugPrint2f(float  v1, float v2) {
    debugPrint1f(v1); writeChar(','); debugPrint1f(v2);
}
void debugPrint3f(float3 v) {
    debugPrint1f(v.x); writeChar(','); debugPrint1f(v.y); writeChar(','); debugPrint1f(v.z);
}
void debugPrint3f(float  v1, float v2, float v3) {
    debugPrint1f(v1); writeChar(','); debugPrint1f(v2); writeChar(','); debugPrint1f(v3);
}
void debugPrint4f(float4 v) {
    debugPrint1f(v.x); writeChar(','); debugPrint1f(v.y); writeChar(','); debugPrint1f(v.z); writeChar(','); debugPrint1f(v.w);
}
void debugPrint4f(float  v1, float v2, float v3, float v4) {
    debugPrint1f(v1); writeChar(','); debugPrint1f(v2); writeChar(','); debugPrint1f(v3); writeChar(','); debugPrint1f(v4);
}

#endif
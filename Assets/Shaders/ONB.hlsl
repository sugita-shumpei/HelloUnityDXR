#ifndef ONB_HLSL
#define ONB_HLSL
float3x3 onb(float3 n)
{
	float3 absN = abs(n);
	float3 up = float3(0, 0, 1);
	if (absN.z > absN.x && absN.y > absN.x) {
		up = float3(1, 0, 0);
	}
	else if (absN.z > absN.y) {
		up = float3(0, 1, 0);
	}
	// tangent, binormal , normal
	float3 t = normalize(cross(up, n));
	float3 b = cross(n, t);
	return float3x3(t, b, n);
}

float3  onbLocalToWorld(float3 local, float3 n)
{
	float3x3 basis = onb(n);
	return mul(local, basis);
}
float3  onbWorldToLocal(float3 world, float3 n)
{
	float3x3 basis = onb(n);
	return mul(basis, world);
}

#endif
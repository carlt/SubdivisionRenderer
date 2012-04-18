#define MAX_ACC_POINTS 32
#define MOD4(x) ((x)&3)

// fractional_odd, fractional_even, integer, pow2
#define PARTITIONING "fractional_odd"

//----------------------------------------------------------------------------------------
// Standard Variables
//----------------------------------------------------------------------------------------
float4x4	World				: WorldMatrix;
float4x4	WorldViewProj		: WorldViewProjectionMatrix;
float		TessFactor			: TessellationFactor;
Texture2D	Texture				: TextureMap;
bool		EnableTexture		: TexturesEnabled;
bool		FlatShading			: FlatShadingEnabled;
bool		WireFrame			: WireFrameEnabled;

//----------------------------------------------------------------------------------------
// Lighting Variables
//----------------------------------------------------------------------------------------
float4		AmbSpecDiffShini	: AmbientSpecularDiffuseShininess;
float4		LightColor			: DirectionalLightColor;
float3		LightDirection		: DirectionalLightDirection;
float4		Light2Color			: DirectionalLight2Color;
float3		Light2Direction		: DirectionalLight2Direction;
float4		AmbientLight		: AmbientLightColor;
float3		Eye					: CameraPosition;

//----------------------------------------------------------------------------------------
// Buffer storing precomputed valences and prefixes for ACC patches
//----------------------------------------------------------------------------------------
Buffer<uint4> ValencePrefixBuffer : register( t1 );

SamplerState stateLinear
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
};

struct VS_INPUT
{
	float4 position : POSITION;
	float3 normal	: NORMAL;
	float2 texcoord	: TEXCOORD;
};

struct VS_OUTPUT
{
	float4 position : POSITION;
	float3 normal	: NORMAL;
	float2 texcoord	: TEXCOORD;
};

struct HS_CONSTANT_OUTPUT
{
	float edges[4]  : SV_TessFactor;
	float inside[2] : SV_InsideTessFactor;
};

struct HS_OUTPUT
{
	float4 position : POSITION;
	float3 normal	: NORMAL;
	float2 texcoord	: TEXCOORD;
};

struct BEZIER_CONTROL_POINT
{
    float3 position : BEZIERPOS;
	float3 normal	: NORMAL;
};

struct DS_OUTPUT
{
	float4 position : SV_POSITION;
	float3 normal	: NORMAL;
	float2 texcoord	: TEXCOORD1;
	float3 posworld : POSITION;
};

//----------------------------------------------------------------------------------------
// Helper Functions
//----------------------------------------------------------------------------------------
float3 BernsteinBasisBiQuad(float t)
{
    float invT = 1.0f - t;

    return float3( invT * invT,
                   2.0f * t * invT,
                   t * t);
}

float4 BernsteinBasisBiCubic(float t)
{
    float invT = 1.0f - t;

    return float4( invT * invT * invT,
                   3.0f * t * invT * invT,
                   3.0f * t * t * invT,
                   t * t * t );
}

float4 dBernsteinBasisBiCubic(float t)
{
    float invT = 1.0f - t;

    return float4( -3 * invT * invT,
                   3 * invT * invT - 6 * t * invT,
                   6 * t * invT - 3 * t * t,
                   3 * t * t );
}

float3 EvaluateBezierBiQuad( float3  p0, float3  p1, float3  p2, 
							 float3  p3, float3  p4, float3  p5,
							 float3  p6, float3  p7, float3  p8,
							 float3 BasisU, float3 BasisV )	
{
    float3 Value;
    Value  = BasisV.x * (  p0 * BasisU.x +  p1 * BasisU.y +  p2 * BasisU.z );
    Value += BasisV.y * (  p3 * BasisU.x +  p4 * BasisU.y +  p5 * BasisU.z );
    Value += BasisV.z * (  p6 * BasisU.x +  p7 * BasisU.y +  p8 * BasisU.z );
    return Value;
}

float4 EvaluateBezierBiCubic(float3  p0, float3  p1, float3  p2, float3  p3,
							 float3  p4, float3  p5, float3  p6, float3  p7,
							 float3  p8, float3  p9, float3 p10, float3 p11,
							 float3 p12, float3 p13, float3 p14, float3 p15,
							 float4 BasisU, float4 BasisV )	
{
    float3 Value;
    Value  = BasisV.x * (  p0 * BasisU.x +  p1 * BasisU.y +  p2 * BasisU.z +  p3 * BasisU.w );
    Value += BasisV.y * (  p4 * BasisU.x +  p5 * BasisU.y +  p6 * BasisU.z +  p7 * BasisU.w );
    Value += BasisV.z * (  p8 * BasisU.x +  p9 * BasisU.y + p10 * BasisU.z + p11 * BasisU.w );
    Value += BasisV.w * ( p12 * BasisU.x + p13 * BasisU.y + p14 * BasisU.z + p15 * BasisU.w );
    return float4(Value, 1);
}

float4 PhongLighting(float3 normal, float3 position)
{
	float3 v = normalize(Eye - position);

	float4 Ia = AmbSpecDiffShini.x * AmbientLight;
	float4 Id = AmbSpecDiffShini.z * saturate(dot(normal, -LightDirection));
	float4 Is = AmbSpecDiffShini.y * pow(saturate(dot(reflect(LightDirection, normal), v)), AmbSpecDiffShini.w);

	float4 Id2 = AmbSpecDiffShini.z * saturate(dot(normal, -Light2Direction));
	float4 Is2 = AmbSpecDiffShini.y * pow(saturate(dot(reflect(Light2Direction, normal), v)), AmbSpecDiffShini.w);

	return Ia + ((Id + Is) * LightColor) + ((Id2 + Is2) * Light2Color);
}

//--------------------------------------------------------------------------------------
// Load per-patch valence and prefix data
//--------------------------------------------------------------------------------------
void LoadValenceAndPrefixData( in uint PatchID, out uint Val[4], out uint Prefixes[4] )
{
    //PatchID += g_iPatchStartIndex;
    uint4 ValPack = ValencePrefixBuffer.Load( PatchID * 2 );
    uint4 PrefPack = ValencePrefixBuffer.Load( PatchID * 2 + 1 );
    
    Val[0] = ValPack.x;
    Val[1] = ValPack.y;
    Val[2] = ValPack.z;
    Val[3] = ValPack.w;
    
    Prefixes[0] = PrefPack.x;
    Prefixes[1] = PrefPack.y;
    Prefixes[2] = PrefPack.z;
    Prefixes[3] = PrefPack.w;
}

//----------------------------------------------------------------------------------------
// Pass-Through Vertex Shader
//----------------------------------------------------------------------------------------
VS_OUTPUT VS(VS_INPUT input)
{
	VS_OUTPUT output;

	output.position = input.position;
	output.normal	= input.normal;
	output.texcoord = input.texcoord;

	return output;
}

//----------------------------------------------------------------------------------------
// Flat Tesselation Shaders
//----------------------------------------------------------------------------------------
HS_CONSTANT_OUTPUT HSCONSTANT_FLAT(InputPatch<VS_OUTPUT, 4> ip, uint pid : SV_PrimitiveID)
{
	HS_CONSTANT_OUTPUT output;
	
	float edge = TessFactor;
	float inside = TessFactor;

	output.edges[0] = edge;
	output.edges[1] = edge;
	output.edges[2] = edge;
	output.edges[3] = edge;

	output.inside[0] = inside;
	output.inside[1] = inside;

	return output;
}

[domain("quad")]
[partitioning(PARTITIONING)]
[outputtopology("triangle_cw")]
[outputcontrolpoints(4)]
[patchconstantfunc("HSCONSTANT_FLAT")]
HS_OUTPUT HS_FLAT(InputPatch<VS_OUTPUT, 4> ip, uint cpid : SV_OutputControlPointID, uint pid : SV_PrimitiveID)
{
	HS_OUTPUT output;

    output.position = ip[cpid].position;
	output.normal	= ip[cpid].normal;
	output.texcoord = ip[cpid].texcoord;

    return output;
}

[domain("quad")]
DS_OUTPUT DS_FLAT(HS_CONSTANT_OUTPUT input, float2 UV : SV_DomainLocation, const OutputPatch<HS_OUTPUT, 4> patch)
{
    DS_OUTPUT output;
    
	// Generate Vertex via bilinear interpolation; same with normals and texcoords
    float3 topMidPos	= lerp(patch[0].position.xyz, patch[1].position.xyz, UV.x);
    float3 botMidPos	= lerp(patch[3].position.xyz, patch[2].position.xyz, UV.x);
    
	float3 topMidNorm	= lerp(patch[0].normal, patch[1].normal, UV.x);
	float3 botMidNorm	= lerp(patch[3].normal, patch[2].normal, UV.x);
	
	float2 topMidTex	= lerp(patch[0].texcoord, patch[1].texcoord, UV.x);
	float2 botMidTex	= lerp(patch[3].texcoord, patch[2].texcoord, UV.x);
	
	float4 newVertex	= float4(lerp(topMidPos, botMidPos, UV.y), 1);

	output.posworld		= mul(newVertex, World).xyz;
	output.position		= mul(newVertex, WorldViewProj);
	output.normal		= lerp(topMidNorm, botMidNorm, UV.y);
	output.texcoord		= lerp(topMidTex, botMidTex, UV.y);

	output.normal		= normalize(mul(output.normal, (float3x3) World));

    return output;    
}

//----------------------------------------------------------------------------------------
// Phong Tesselation Shaders
//----------------------------------------------------------------------------------------
float3 PhongOperator(float3 p, float3 c, float3 n)
{
	return p - dot(p - c, n) * n;
}

[domain("quad")]
DS_OUTPUT DS_PHONG(HS_CONSTANT_OUTPUT input, float2 UV : SV_DomainLocation, const OutputPatch<HS_OUTPUT, 4> patch)
{
    DS_OUTPUT output;
    
	// Bilinear interpolation of position
    float3 topMidPos	= lerp(patch[0].position.xyz, patch[1].position.xyz, UV.x);
    float3 botMidPos	= lerp(patch[3].position.xyz, patch[2].position.xyz, UV.x);
    	
	float3 newVertex	= lerp(topMidPos, botMidPos, UV.y);

	// Phong operator and bilinear interpolation of results for new position
	float3 c0			= PhongOperator(newVertex, patch[0].position.xyz, patch[0].normal);
	float3 c1			= PhongOperator(newVertex, patch[1].position.xyz, patch[1].normal);
	float3 c2			= PhongOperator(newVertex, patch[2].position.xyz, patch[2].normal);
	float3 c3			= PhongOperator(newVertex, patch[3].position.xyz, patch[3].normal);

	float3 phongTopMid	= lerp(c0, c1, UV.x);
	float3 phongBotMid	= lerp(c3, c2, UV.x);

	float3 phongNew		= lerp(newVertex, lerp(phongTopMid, phongBotMid, UV.y), 0.75f /* alpha parameter, default = 3/4 */);

	output.posworld		= mul(phongNew, (float3x3) World);
	output.position		= mul(float4(phongNew, 1), WorldViewProj);

	// Bilinear interpolation of normals
	float3 topMidNorm	= lerp(patch[0].normal, patch[1].normal, UV.x);
    float3 botMidNorm	= lerp(patch[3].normal, patch[2].normal, UV.x);

	output.normal		= lerp(topMidNorm, botMidNorm, UV.y);
	output.normal		= normalize(mul(output.normal, (float3x3) World));

	// Bilinear interpolation of texcoords
	float2 topMidTex	= lerp(patch[0].texcoord, patch[1].texcoord, UV.x);
	float2 botMidTex	= lerp(patch[3].texcoord, patch[2].texcoord, UV.x);
	
	output.texcoord		= lerp(topMidTex, botMidTex, UV.y);

    return output;    
}

//----------------------------------------------------------------------------------------
// PN Quads Tesselation Shaders
//----------------------------------------------------------------------------------------
[domain("quad")]
[partitioning(PARTITIONING)]
[outputtopology("triangle_cw")]
[outputcontrolpoints(4)]
[patchconstantfunc("HSCONSTANT_PNQUAD")]
HS_OUTPUT HS_PNQUAD(InputPatch<VS_OUTPUT, 4> ip, uint cpid : SV_OutputControlPointID, uint pid : SV_PrimitiveID)
{
	HS_OUTPUT output;

    output.position = ip[cpid].position;
	output.normal	= ip[cpid].normal;
	output.texcoord = ip[cpid].texcoord;

    return output;
}

float3 Bij(float3 pi, float3 pj, float3 ni)
{
	return (2.0 * pi + pj - dot(pj - pi, ni) * ni) / 3.0;
}

float3 Nij(float3 pi, float3 pj, float3 ni, float3 nj)
{
	float3 d = pj - pi;
	float vij = 2.0 * dot(normalize(d), normalize(ni+nj));
	return normalize(ni + nj - vij * d);
}

struct HSCONSTANT_PNQUAD_OUTPUT
{
    float Edges[4]			: SV_TessFactor;
    float Inside[2]			: SV_InsideTessFactor;
    float3 EdgePos[8]		: EDGEPOS;				// Order: b01, b10, b12, b21, b23, b32, b30, b03
    float3 InteriorPos[4]	: INTERIORPOS;			// Order: b02, b13, b20, b31
	float3 EdgeNormals[4]	: EDGENORMALS;			// Order: n01 = n10, n12 = n21, n23 = n32, n30 = n03
	float3 CenterNormal		: CENTERNORMAL;			// n0123
};

HSCONSTANT_PNQUAD_OUTPUT HSCONSTANT_PNQUAD(InputPatch<VS_OUTPUT, 4> inputPatch, uint pid : SV_PrimitiveID)
{
	HSCONSTANT_PNQUAD_OUTPUT output;
	
	float edge = TessFactor;
	float inside = TessFactor;

	output.Edges[0] = edge;
	output.Edges[1] = edge;
	output.Edges[2] = edge;
	output.Edges[3] = edge;

	output.Inside[0] = inside;
	output.Inside[1] = inside;

	output.EdgePos[0] = Bij(inputPatch[0].position.xyz, inputPatch[1].position.xyz, inputPatch[0].normal);
	output.EdgePos[1] = Bij(inputPatch[1].position.xyz, inputPatch[0].position.xyz, inputPatch[1].normal);
	output.EdgePos[2] = Bij(inputPatch[1].position.xyz, inputPatch[2].position.xyz, inputPatch[1].normal);
	output.EdgePos[3] = Bij(inputPatch[2].position.xyz, inputPatch[1].position.xyz, inputPatch[2].normal);
	output.EdgePos[4] = Bij(inputPatch[2].position.xyz, inputPatch[3].position.xyz, inputPatch[2].normal);
	output.EdgePos[5] = Bij(inputPatch[3].position.xyz, inputPatch[2].position.xyz, inputPatch[3].normal);
	output.EdgePos[6] = Bij(inputPatch[3].position.xyz, inputPatch[0].position.xyz, inputPatch[3].normal);
	output.EdgePos[7] = Bij(inputPatch[0].position.xyz, inputPatch[3].position.xyz, inputPatch[0].normal);

    float3 q = output.EdgePos[0];
    for (int i = 1; i < 8; ++i)
    {
        q += output.EdgePos[i];
    }

    float3 center = inputPatch[0].position.xyz + inputPatch[1].position.xyz + inputPatch[2].position.xyz + inputPatch[3].position.xyz;

	// 4 Inside Positions
	[unroll]
    for (i = 0; i < 4; ++i)
    {
        float3 Ei = (2 * (output.EdgePos[i * 2] + output.EdgePos[((i + 3) & 3) * 2 + 1] + q) - (output.EdgePos[((i + 1) & 3) * 2 + 1] + output.EdgePos[((i + 2) & 3) * 2])) / 18;
        float3 Vi = (center + 2 * (inputPatch[(i + 3) & 3].position.xyz + inputPatch[(i + 1) & 3].position.xyz) + inputPatch[(i + 2) & 3].position.xyz) / 9;
        
		output.InteriorPos[i] = 1.5 * Ei - 0.5 * Vi;
    }

	output.EdgeNormals[0] = Nij(inputPatch[0].position.xyz, inputPatch[1].position.xyz, inputPatch[0].normal, inputPatch[1].normal);
	output.EdgeNormals[1] = Nij(inputPatch[1].position.xyz, inputPatch[2].position.xyz, inputPatch[1].normal, inputPatch[2].normal);
	output.EdgeNormals[2] = Nij(inputPatch[2].position.xyz, inputPatch[3].position.xyz, inputPatch[2].normal, inputPatch[3].normal);
	output.EdgeNormals[3] = Nij(inputPatch[3].position.xyz, inputPatch[0].position.xyz, inputPatch[3].normal, inputPatch[0].normal);

	output.CenterNormal = 2.0 * (output.EdgeNormals[0] + output.EdgeNormals[1] + output.EdgeNormals[2] + output.EdgeNormals[3]); 
	output.CenterNormal += inputPatch[0].normal + inputPatch[1].normal + inputPatch[2].normal + inputPatch[3].normal;
	output.CenterNormal /= 12;
	output.CenterNormal = normalize(output.CenterNormal);

    return output;
}

[domain("quad")]
DS_OUTPUT DS_PNQUAD(HSCONSTANT_PNQUAD_OUTPUT input, float2 uv : SV_DomainLocation, OutputPatch<HS_OUTPUT, 4> inputPatch)
{
	DS_OUTPUT output = (DS_OUTPUT) 0;

	// Evaluate BiCubic Bezier for position
	float4 basisUCubic = BernsteinBasisBiCubic(uv.x);
    float4 basisVCubic = BernsteinBasisBiCubic(uv.y);

	output.position = EvaluateBezierBiCubic( inputPatch[0].position.xyz,	input.EdgePos[0],		input.EdgePos[1],		inputPatch[1].position.xyz,
											 input.EdgePos[7],				input.InteriorPos[0],	input.InteriorPos[1],	input.EdgePos[2],
											 input.EdgePos[6],				input.InteriorPos[3],	input.InteriorPos[2],	input.EdgePos[3],
											 inputPatch[3].position.xyz,	input.EdgePos[5],		input.EdgePos[4],		inputPatch[2].position.xyz,
											 basisUCubic,					basisVCubic);
	
	// Evaluate BiQuadratic Bezier for normal
	float3 basisUQuad = BernsteinBasisBiQuad(uv.x);
	float3 basisVQuad = BernsteinBasisBiQuad(uv.y);

	output.normal = EvaluateBezierBiQuad(	inputPatch[0].normal,	input.EdgeNormals[0],	inputPatch[1].normal,
											input.EdgeNormals[3],	input.CenterNormal,		input.EdgeNormals[2],
											inputPatch[3].normal,	input.EdgeNormals[2],	inputPatch[2].normal,
											basisUQuad,				basisVQuad);

	// Bilinear interpolation of texcoords
	float2 topMidTex	= lerp(inputPatch[0].texcoord, inputPatch[1].texcoord, uv.x);
	float2 botMidTex	= lerp(inputPatch[3].texcoord, inputPatch[2].texcoord, uv.x);
	
	output.texcoord		= lerp(topMidTex, botMidTex, uv.y);

	output.normal	= normalize(mul(output.normal, (float3x3) World));
	output.posworld = mul(output.position, World).xyz;
	output.position = mul(output.position, WorldViewProj);

	return output;
}

//----------------------------------------------------------------------------------------
// ACC Tesselation Shaders
//----------------------------------------------------------------------------------------
float4 ComputeInteriorVertex( uint index, 
                              uint Val[4], 
                              const in InputPatch<VS_OUTPUT, MAX_ACC_POINTS> ip )
{
    switch( index )
    {
    case 0:
        return (ip[0].position*Val[0] + ip[1].position*2 +      ip[2].position +        ip[3].position*2)      / (5+Val[0]);
    case 1:
        return (ip[0].position*2 +      ip[1].position*Val[1] + ip[2].position*2 +      ip[3].position)        / (5+Val[1]);
    case 2:
        return (ip[0].position +        ip[1].position*2 +      ip[2].position*Val[2] + ip[3].position*2)      / (5+Val[2]);
    case 3:
        return (ip[0].position*2 +      ip[1].position +        ip[2].position*2 +      ip[3].position*Val[3]) / (5+Val[3]);
    }
    
    return float4(0,0,0,0);
}

float3 ComputeInteriorNormal( uint index, 
                              uint Val[4], 
                              const in InputPatch<VS_OUTPUT, MAX_ACC_POINTS> ip )
{
    switch( index )
    {
    case 0:
        return (ip[0].normal*Val[0] + ip[1].normal*2 +      ip[2].normal +        ip[3].normal*2)      / (5+Val[0]);
    case 1:
        return (ip[0].normal*2 +      ip[1].normal*Val[1] + ip[2].normal*2 +      ip[3].normal)        / (5+Val[1]);
    case 2:
        return (ip[0].normal +        ip[1].normal*2 +      ip[2].normal*Val[2] + ip[3].normal*2)      / (5+Val[2]);
    case 3:
        return (ip[0].normal*2 +      ip[1].normal +        ip[2].normal*2 +      ip[3].normal*Val[3]) / (5+Val[3]);
    }
    
    return float3(0,0,0);
}

//--------------------------------------------------------------------------------------
// Computes the corner vertices of the output UV patch.  The corner vertices are
// a weighted combination of all points that are "connected" to that corner by an edge.
// The interior 4 points of the original subd quad are easy to get.  The points in the
// 1-ring neighborhood around the interior quad are not.
//
// Because the valence of that corner could be any number between 3 and 16, we need to
// walk around the subd patch vertices connected to that point.  This is there the
// Pref (prefix) values come into play.  Each corner has a prefix value that is the index
// of the last value around the 1-ring neighborhood that should be used in calculating
// the coefficient of that corner.  The walk goes from the prefix value of the previous
// corner to the prefix value of the current corner.
//--------------------------------------------------------------------------------------
void ComputeCorner( uint index, 
                    out float3 CornerB, // Corner for the Bezier patch
                    out float3 CornerN, // Corner for the normal patch
                    const in InputPatch<VS_OUTPUT, MAX_ACC_POINTS> ip, 
                    const in uint Val[4], 
                    const in uint Pref[4] )
{
    const float fOWt = 1;
    const float fEWt = 4;

    // Figure out where to start the walk by using the previous corner's prefix value
    uint PrefIm1 = 0;
    uint uStart = 4;
    if( index )
    {
        PrefIm1 = Pref[index-1];
        uStart = PrefIm1;
    }

    // Calculate the N*N weight for the final value
    CornerB = (Val[index]*Val[index])*ip[index].position.xyz; // n^2 part
	CornerN = (Val[index]*Val[index])*ip[index].normal;
        
    // Start the walk with the uStart prefix (the prefix of the corner before us)
    CornerB += ip[uStart].position.xyz * fEWt;
	CornerN += ip[uStart].normal * fEWt;
    
    // Gather all vertices between the previous corner's prefix and our own prefix
    // We'll do two at a time, since they always come in twos
    while(uStart < Pref[index]-1) 
    {
        ++uStart;
        CornerB += ip[uStart].position.xyz * fOWt;
		CornerN += ip[uStart].normal * fOWt;

        ++uStart;
        CornerB += ip[uStart].position.xyz * fEWt;
		CornerN += ip[uStart].normal * fEWt;
    }
    ++uStart;

    // Add in the last guy and make sure to wrap to the beginning if we're the last corner
    if (index == 3)
        uStart = 4; 
    CornerB += ip[uStart].position.xyz * fOWt;
	CornerN += ip[uStart].normal * fOWt;

    // Add in the guy before the prefix as well
    if (index)
        uStart = PrefIm1-1;
    else
        uStart = Pref[3]-1;

    CornerB += ip[uStart].position.xyz * fOWt;
	CornerN += ip[uStart].normal * fOWt;

    // We're done with the walk now.  Now we need to add the contributions of the original subd quad.
    CornerB += ip[MOD4(index+1)].position.xyz * fEWt;
    CornerB += ip[MOD4(index+2)].position.xyz * fOWt;
    CornerB += ip[MOD4(index+3)].position.xyz * fEWt;

	CornerN += ip[MOD4(index+1)].normal * fEWt;
    CornerN += ip[MOD4(index+2)].normal * fOWt;
    CornerN += ip[MOD4(index+3)].normal * fEWt;
    
    // Normalize the corner weights
    CornerB *= 1.0f / ( Val[index] * Val[index] + 5 * Val[index] ); // normalize
	CornerN *= 1.0f / ( Val[index] * Val[index] + 5 * Val[index] );
}

//--------------------------------------------------------------------------------------
// Computes the edge vertices of the output bicubic patch.  The edge vertices
// (1,2,4,7,8,11,13,14) are a weighted (by valence) combination of 6 interior and 1-ring
// neighborhood points.  However, we don't have to do the walk on this one since we
// don't need all of the neighbor points attached to this vertex.
//--------------------------------------------------------------------------------------
float3 ComputeEdgeVertex( in uint index /* 0-7 */, 
                          const in InputPatch<VS_OUTPUT, MAX_ACC_POINTS> ip, 
                          const in uint Val[4], 
                          const in uint Pref[4] )
{
    float val1 = 2 * Val[0] + 10;
    float val2 = 2 * Val[1] + 10;
    float val13 = 2 * Val[3] + 10;
    float val14 = 2 * Val[2] + 10;
    float val4 = val1;
    float val8 = val13;
    float val7 = val2;
    float val11 = val14;
    
    float3 vRetVal = float3(0,0,0);
    switch( index )
    {
    // Horizontal
    case 0:
        vRetVal = (Val[0]*2*ip[0].position.xyz + 4*ip[1].position.xyz + ip[2].position.xyz + ip[3].position.xyz*2 +
              2*ip[Pref[0]-1].position.xyz + ip[Pref[0]].position.xyz) / val1;
        break;
    case 1:
        vRetVal = (4*ip[0].position.xyz + Val[1]*2*ip[1].position.xyz + ip[2].position.xyz*2 + ip[3].position.xyz +
              ip[Pref[0]-1].position.xyz + 2*ip[Pref[0]].position.xyz) / val2;
        break;
    case 2:
        vRetVal = (2*ip[0].position.xyz + ip[1].position.xyz + 4*ip[2].position.xyz + ip[3].position.xyz*2*Val[3] +
               2*ip[Pref[2]].position.xyz + ip[Pref[2]-1].position.xyz) / val13;
        break;
    case 3:
        vRetVal = (ip[0].position.xyz + 2*ip[1].position.xyz + Val[2]*2*ip[2].position.xyz + ip[3].position.xyz*4 +
               ip[Pref[2]].position.xyz + 2*ip[Pref[2]-1].position.xyz) / val14;
        break;
    // Vertical
    case 4:
        vRetVal = (Val[0]*2*ip[0].position.xyz + 2*ip[1].position.xyz + ip[2].position.xyz + ip[3].position.xyz*4 +
              2*ip[4].position.xyz + ip[Pref[3]-1].position.xyz) / val4;
        break;
    case 5:
        vRetVal = (4*ip[0].position.xyz + ip[1].position.xyz + 2*ip[2].position.xyz + ip[3].position.xyz*2*Val[3] +
              ip[4].position.xyz + 2*ip[Pref[3]-1].position.xyz) / val8;
        break;
    case 6:
        vRetVal = (2*ip[0].position.xyz + Val[1]*2*ip[1].position.xyz + 4*ip[2].position.xyz + ip[3].position.xyz +
              2*ip[Pref[1]-1].position.xyz + ip[Pref[1]].position.xyz) / val7;
        break;
    case 7:
        vRetVal = (ip[0].position.xyz + 4*ip[1].position.xyz + Val[2]*2*ip[2].position.xyz + 2*ip[3].position.xyz +
               ip[Pref[1]-1].position.xyz + 2*ip[Pref[1]].position.xyz) / val11;
        break;
    }
        
    return vRetVal;
}

float3 ComputeEdgeNormal( in uint index /* 0-7 */, 
                          const in InputPatch<VS_OUTPUT, MAX_ACC_POINTS> ip, 
                          const in uint Val[4], 
                          const in uint Pref[4] )
{
    float val1 = 2 * Val[0] + 10;
    float val2 = 2 * Val[1] + 10;
    float val13 = 2 * Val[3] + 10;
    float val14 = 2 * Val[2] + 10;
    float val4 = val1;
    float val8 = val13;
    float val7 = val2;
    float val11 = val14;
    
    float3 vRetVal = float3(0,0,0);
    switch( index )
    {
    // Horizontal
    case 0:
        vRetVal = (Val[0]*2*ip[0].normal + 4*ip[1].normal + ip[2].normal + ip[3].normal*2 +
              2*ip[Pref[0]-1].normal + ip[Pref[0]].normal) / val1;
        break;
    case 1:
        vRetVal = (4*ip[0].normal + Val[1]*2*ip[1].normal + ip[2].normal*2 + ip[3].normal +
              ip[Pref[0]-1].normal + 2*ip[Pref[0]].normal) / val2;
        break;
    case 2:
        vRetVal = (2*ip[0].normal + ip[1].normal + 4*ip[2].normal + ip[3].normal*2*Val[3] +
               2*ip[Pref[2]].normal + ip[Pref[2]-1].normal) / val13;
        break;
    case 3:
        vRetVal = (ip[0].normal + 2*ip[1].normal + Val[2]*2*ip[2].normal + ip[3].normal*4 +
               ip[Pref[2]].normal + 2*ip[Pref[2]-1].normal) / val14;
        break;
    // Vertical
    case 4:
        vRetVal = (Val[0]*2*ip[0].normal + 2*ip[1].normal + ip[2].normal + ip[3].normal*4 +
              2*ip[4].normal + ip[Pref[3]-1].normal) / val4;
        break;
    case 5:
        vRetVal = (4*ip[0].normal + ip[1].normal + 2*ip[2].normal + ip[3].normal*2*Val[3] +
              ip[4].normal + 2*ip[Pref[3]-1].normal) / val8;
        break;
    case 6:
        vRetVal = (2*ip[0].normal + Val[1]*2*ip[1].normal + 4*ip[2].normal + ip[3].normal +
              2*ip[Pref[1]-1].normal + ip[Pref[1]].normal) / val7;
        break;
    case 7:
        vRetVal = (ip[0].normal + 4*ip[1].normal + Val[2]*2*ip[2].normal + 2*ip[3].normal +
               ip[Pref[1]-1].normal + 2*ip[Pref[1]].normal) / val11;
        break;
    }
        
    return vRetVal;
}

struct HS_CONSTANT_ACC_OUTPUT
{
    float Edges[4]			: SV_TessFactor;
    float Inside[2]			: SV_InsideTessFactor;
    
	float2 texcoords[4]		: TEXCOORD;
};

HS_CONSTANT_ACC_OUTPUT HSCONSTANT_ACC(InputPatch<VS_OUTPUT, MAX_ACC_POINTS> ip,
                                       uint PatchID : SV_PrimitiveID )
{	
    HS_CONSTANT_ACC_OUTPUT Output;
    
    float TessAmount = TessFactor;

    Output.Edges[0] = Output.Edges[1] = Output.Edges[2] = Output.Edges[3] = TessAmount;
    Output.Inside[0] = Output.Inside[1] = TessAmount;
    
    Output.texcoords[0] = ip[0].texcoord;
    Output.texcoords[1] = ip[1].texcoord;
    Output.texcoords[2] = ip[2].texcoord;
    Output.texcoords[3] = ip[3].texcoord;

    return Output;
}

//--------------------------------------------------------------------------------------
// HS for ACC.  This outputcontrolpoints(16) specifies that we will produce
// 16 control points.  Therefore this function will be invoked 16x, one for each output
// control point.
//
// !! PERFORMANCE NOTE: This hull shader is written for maximum readability, and its
// performance is not expected to be optimal on D3D11 hardware.  The switch statement
// below that determines the codepath for each patch control point generates sub-optimal
// code for parallel execution on the GPU.  A future implementation of this hull shader
// will combine the 16 codepaths and 3 variants (corner, edge, interior) into one shared
// codepath; this change is expected to increase performance at the expense of readability.
//--------------------------------------------------------------------------------------
[domain("quad")]
[partitioning(PARTITIONING)]
[outputtopology("triangle_cw")]
[outputcontrolpoints(16)]
[patchconstantfunc("HSCONSTANT_ACC")]
BEZIER_CONTROL_POINT HS_ACC(InputPatch<VS_OUTPUT, MAX_ACC_POINTS> p, 
                            uint i : SV_OutputControlPointID,
                            uint PatchID : SV_PrimitiveID )
{
    // Valences and prefixes are loaded from a buffer
    uint Val[4];
    uint Prefixes[4];
    LoadValenceAndPrefixData( PatchID, Val, Prefixes );
    
    float3 CornerB = float3(0,0,0);
    float3 CornerN = float3(0,0,0);

    BEZIER_CONTROL_POINT Output;
    Output.position = float3(0,0,0);
    
    // !! PERFORMANCE NOTE: As mentioned above, this switch statement generates
    // inefficient code for the sake of readability.
    switch( i )
    {
    // Interior vertices
    case 5:
        Output.position = ComputeInteriorVertex( 0, Val, p ).xyz;
		Output.normal = ComputeInteriorNormal( 0, Val, p );
        break;
    case 6:
        Output.position = ComputeInteriorVertex( 1, Val, p ).xyz;
		Output.normal = ComputeInteriorNormal( 1, Val, p );
        break;
    case 10:
        Output.position = ComputeInteriorVertex( 2, Val, p ).xyz;
		Output.normal = ComputeInteriorNormal( 2, Val, p );
        break;
    case 9:
        Output.position = ComputeInteriorVertex( 3, Val, p ).xyz;
		Output.normal = ComputeInteriorNormal( 3, Val, p );
        break;
        
    // Corner vertices
    case 0:
        ComputeCorner( 0, CornerB, CornerN, p, Val, Prefixes );
        Output.position = CornerB;
		Output.normal = CornerN;
        break;
    case 3:
        ComputeCorner( 1, CornerB, CornerN, p, Val, Prefixes );
        Output.position = CornerB;
		Output.normal = CornerN;
        break;
    case 15:
        ComputeCorner( 2, CornerB, CornerN, p, Val, Prefixes );
        Output.position = CornerB;
		Output.normal = CornerN;
        break;
    case 12:
        ComputeCorner( 3, CornerB, CornerN, p, Val, Prefixes );
        Output.position = CornerB;
		Output.normal = CornerN;
        break;
        
    // Edge vertices
    case 1:
        Output.position = ComputeEdgeVertex( 0, p, Val, Prefixes );
		Output.normal = ComputeEdgeNormal( 0, p, Val, Prefixes );
        break;
    case 2:
        Output.position = ComputeEdgeVertex( 1, p, Val, Prefixes );
		Output.normal = ComputeEdgeNormal( 1, p, Val, Prefixes );
        break;
    case 13:
        Output.position = ComputeEdgeVertex( 2, p, Val, Prefixes );
		Output.normal = ComputeEdgeNormal( 2, p, Val, Prefixes );
        break;
    case 14:
        Output.position = ComputeEdgeVertex( 3, p, Val, Prefixes );
		Output.normal = ComputeEdgeNormal( 3, p, Val, Prefixes );
        break;
    case 4:
        Output.position = ComputeEdgeVertex( 4, p, Val, Prefixes );
		Output.normal = ComputeEdgeNormal( 4, p, Val, Prefixes );
        break;
    case 8:
        Output.position = ComputeEdgeVertex( 5, p, Val, Prefixes );
		Output.normal = ComputeEdgeNormal( 5, p, Val, Prefixes );
        break;
    case 7:
        Output.position = ComputeEdgeVertex( 6, p, Val, Prefixes );
		Output.normal = ComputeEdgeNormal( 6, p, Val, Prefixes );
        break;
    case 11:
        Output.position = ComputeEdgeVertex( 7, p, Val, Prefixes );
		Output.normal = ComputeEdgeNormal( 7, p, Val, Prefixes );
        break;
    }
    
    return Output;
}

[domain("quad")]
DS_OUTPUT DS_ACC( HS_CONSTANT_ACC_OUTPUT input, 
                        float2 UV : SV_DomainLocation,
                        const OutputPatch<BEZIER_CONTROL_POINT, 16> bezpatch )
{
    float4 BasisU = BernsteinBasisBiCubic( UV.x );
    float4 BasisV = BernsteinBasisBiCubic( UV.y );
    
    float4 WorldPos = EvaluateBezierBiCubic( bezpatch[0].position,  bezpatch[1].position,  bezpatch[2].position,  bezpatch[3].position,
											 bezpatch[4].position,  bezpatch[5].position,  bezpatch[6].position,  bezpatch[7].position,
											 bezpatch[8].position,  bezpatch[9].position,  bezpatch[10].position, bezpatch[11].position,
											 bezpatch[12].position, bezpatch[13].position, bezpatch[14].position, bezpatch[15].position,
											 BasisU,				BasisV);

	float3 Normal	= EvaluateBezierBiCubic( bezpatch[0].normal,  bezpatch[1].normal,  bezpatch[2].normal,  bezpatch[3].normal,
											 bezpatch[4].normal,  bezpatch[5].normal,  bezpatch[6].normal,  bezpatch[7].normal,
											 bezpatch[8].normal,  bezpatch[9].normal,  bezpatch[10].normal, bezpatch[11].normal,
											 bezpatch[12].normal, bezpatch[13].normal, bezpatch[14].normal, bezpatch[15].normal,
											 BasisU,				BasisV);

    DS_OUTPUT Output;
    Output.normal = Normal;

    // bilerp the texture coordinates    
    float2 tex0 = input.texcoords[0];
    float2 tex1 = input.texcoords[1];
    float2 tex2 = input.texcoords[2];
    float2 tex3 = input.texcoords[3];
        
    float2 bottom = lerp( tex0, tex1, UV.x );
    float2 top = lerp( tex3, tex2, UV.x );
    float2 TexUV = lerp( bottom, top, UV.y );
    Output.texcoord = TexUV;
    
	Output.normal   = mul(Output.normal, (float3x3) World);
    Output.position = mul(WorldPos, WorldViewProj );
    Output.posworld = mul(WorldPos.xyz, (float3x3) World);
    
    return Output;    
}

//----------------------------------------------------------------------------------------
// Pixel Shaders & Techniques
//----------------------------------------------------------------------------------------
float4 PS(DS_OUTPUT input) : SV_Target
{
	if (WireFrame) return float4(0,0,0,0);

	if (FlatShading)
	{
		float3 xdir = ddx(input.posworld);
		float3 ydir = ddy(input.posworld);
		input.normal = normalize(cross(xdir, ydir));
	} 
	else 
	{
		input.normal = normalize(input.normal);
	}

	if (EnableTexture)
		return PhongLighting(input.normal, input.posworld) * Texture.Sample(stateLinear, input.texcoord);
	
	return PhongLighting(input.normal, input.posworld);
}

technique11 RenderFlat
{
	pass P0
	{
		SetGeometryShader(0);

		SetVertexShader	(CompileShader(vs_5_0, VS()));
		SetHullShader	(CompileShader(hs_5_0, HS_FLAT()));
		SetDomainShader	(CompileShader(ds_5_0, DS_FLAT()));
		SetPixelShader	(CompileShader(ps_5_0, PS()));
	}
}

technique11 RenderPhongTess
{
	pass P0
	{
		SetGeometryShader(0);

		SetVertexShader	(CompileShader(vs_5_0, VS()));
		SetHullShader	(CompileShader(hs_5_0, HS_FLAT()));
		SetDomainShader	(CompileShader(ds_5_0, DS_PHONG()));
		SetPixelShader	(CompileShader(ps_5_0, PS()));
	}
}

technique11 RenderPNQuads
{
	pass P0
	{
		SetGeometryShader(0);

		SetVertexShader	(CompileShader(vs_5_0, VS()));
		SetHullShader	(CompileShader(hs_5_0, HS_PNQUAD()));
		SetDomainShader	(CompileShader(ds_5_0, DS_PNQUAD()));
		SetPixelShader	(CompileShader(ps_5_0, PS()));
	}
}

technique11 RenderACC
{
	pass P0
	{
		SetGeometryShader(0);

		SetVertexShader	(CompileShader(vs_5_0, VS()));
		SetHullShader	(CompileShader(hs_5_0, HS_ACC()));
		SetDomainShader	(CompileShader(ds_5_0, DS_ACC()));
		SetPixelShader	(CompileShader(ps_5_0, PS()));
	}
}
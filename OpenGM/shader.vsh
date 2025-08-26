#version 330 core // 460 has new stuff for gl_fragcolor etc. dont feel like changing it

#define MATRIX_VIEW 0
#define MATRIX_PROJECTION 1
#define MATRIX_WORLD 2
#define MATRIX_WORLD_VIEW 3
#define MATRIX_WORLD_VIEW_PROJECTION 4
#define MATRICES_MAX 5
#define FOG_SETTINGS 0
#define FOG_COLOUR 1

layout (location = 0) in vec3 a_pos;
layout (location = 1) in vec4 a_color;
layout (location = 2) in vec2 a_uv;

out vec4 fcolor;
out vec2 texc;
out float fogFactor;

uniform mat4 gm_Matrices[MATRICES_MAX];
uniform float gm_FogStart;
uniform float gm_RcpFogRange;
uniform bool gm_VS_FogEnabled;

float CalcFogFactor(vec4 pos)
{
	if (gm_VS_FogEnabled)
	{
		vec4 viewpos = gm_Matrices[MATRIX_WORLD_VIEW] * pos;
		float fogfactor = ((viewpos.z - gm_FogStart) * gm_RcpFogRange);
		return fogfactor;
	}
	else
	{
		return 0.0;
	}
}

void main() {
    fcolor = a_color;
    texc = a_uv;
    vec4 pos = vec4(a_pos.xyz, 1);
    fogFactor = CalcFogFactor(pos);
    gl_Position = gm_Matrices[MATRIX_WORLD_VIEW_PROJECTION] * pos;
    gl_PointSize = 1.0;
}
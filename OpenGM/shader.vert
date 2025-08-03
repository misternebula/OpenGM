#version 330 core

layout (location = 0) in vec2 a_pos;
layout (location = 1) in vec4 a_color;
layout (location = 2) in vec2 a_uv;

out vec4 fcolor;
out vec2 texc;
out float fogFactor;

uniform vec4 u_view; // x, y, width, height
uniform bool u_flipY; // backbuffer drawing should be flipped. nothing else should

uniform float gm_FogStart;
uniform float gm_RcpFogRange;
uniform bool gm_VS_FogEnabled;

float CalcFogFactor(vec4 pos)
{
    if (gm_VS_FogEnabled)
	{
		//vec4 viewpos = gm_Matrices[MATRIX_WORLD_VIEW] * pos;
		vec4 viewpos = pos;
		float fogfactor = ((viewpos.z - gm_FogStart) * gm_RcpFogRange);
		return fogfactor;
	}
	else
	{
		return 0.0;
	}
}

void main() {
    gl_Position.xy = (a_pos - u_view.xy) / u_view.zw; // convert from view to 0..1
    if (u_flipY) gl_Position.y = 1 - gl_Position.y;
    gl_Position.xy = gl_Position.xy * 2 - 1; // 0..1 to -1..1
    gl_Position.zw = vec2(1);
    fcolor = a_color;
    texc = a_uv;
	fogFactor = CalcFogFactor(gl_Position);
}
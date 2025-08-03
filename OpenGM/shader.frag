#version 330 core

uniform bool alphaTestEnabled;
uniform float alphaRefValue;

in vec4 fcolor;
in vec2 texc;
in float fogFactor;

uniform sampler2D u_tex;
uniform bool u_doTex; // maybe i could set the tex uniform to -1 too but idc
uniform bool gm_PS_FogEnabled;
uniform vec4 gm_FogColour;

void DoAlphaTest(vec4 SrcColour)
{
    if (alphaTestEnabled)
    {
        if (SrcColour.a <= alphaRefValue)
        {
            discard;
        }
    }
}

void DoFog(inout vec4 SrcColour, float fogval)
{
	if (gm_PS_FogEnabled)
	{
		SrcColour = mix(SrcColour, gm_FogColour, clamp(fogval, 0.0, 1.0)); 
	}
}


void main() {
    vec4 color = fcolor * (u_doTex ? texture2D(u_tex, texc) : vec4(1));
    DoAlphaTest(color);
    DoFog(color, fogFactor);
    gl_FragColor = color;
}
// 460 has new stuff for gl_fragcolor etc. dont feel like changing it
#version 330 core

in vec4 fcolor;
in vec2 texc;
in float fogFactor;

uniform sampler2D gm_BaseTexture;
uniform bool gm_PS_FogEnabled;
uniform vec4 gm_FogColour;
uniform bool gm_AlphaTestEnabled;
uniform float gm_AlphaRefValue;

void DoAlphaTest(vec4 SrcColour)
{
    if (gm_AlphaTestEnabled)
    {
        if (SrcColour.a <= gm_AlphaRefValue)
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
    vec4 color = texture2D(gm_BaseTexture, texc).rgba * fcolor.rgba;
    DoAlphaTest(color);
    DoFog(color, fogFactor);
    gl_FragColor = color;
}
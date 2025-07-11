#version 330 core

uniform bool gm_AlphaTestEnabled;
uniform float gm_AlphaRefValue;

in vec4 v_vColour;
in vec2 v_vTexcoord;

uniform sampler2D gm_BaseTexture;

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

void main() {
    vec4 color = v_vColour * texture2D(gm_BaseTexture, v_vTexcoord);
    DoAlphaTest(color);
    gl_FragColor = color;
}
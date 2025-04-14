#version 330 core

uniform bool alphaTestEnabled;
uniform float alphaRefValue;

in vec4 fcolor;
in vec2 texc;

uniform sampler2D u_tex;
uniform bool u_doTex; // maybe i could set the tex uniform to -1 too but idc

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

void main() {
    vec4 color = fcolor * (u_doTex ? texture2D(u_tex, texc) : vec4(1));
    DoAlphaTest(color);
    gl_FragColor = color;
}
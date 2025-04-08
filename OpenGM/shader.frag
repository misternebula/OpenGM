#version 330 core

in vec4 color;
in vec2 uv;

uniform sampler2D u_tex;
uniform bool u_doTex; // maybe i could set the tex uniform to -1 too but idc

void main() {
    gl_FragColor = color * (u_doTex ? texture2D(u_tex, uv) : vec4(1));
}
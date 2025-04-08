#version 330 core

in vec4 color;
in vec2 uv;

uniform sampler2D u_tex;

void main() {
    gl_FragColor = color;
}
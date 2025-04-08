#version 330 core

layout (location = 0) in vec2 a_pos;
layout (location = 1) in vec4 a_color;
layout (location = 2) in vec2 a_uv;

out vec4 color;
out vec2 uv;

//uniform vec2 offset;

void main() {
    gl_Position = vec4(a_pos, 1, 1);
    color = a_color;
    uv = a_uv;
}
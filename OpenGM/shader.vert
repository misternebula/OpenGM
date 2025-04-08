#version 330 core

layout (location = 0) in vec2 a_pos;
layout (location = 1) in vec4 a_color;
layout (location = 2) in vec2 a_uv;

out vec4 color;
out vec2 uv;

//uniform vec4 u_view; // x, y, width, height

void main() {
//    gl_Position = vec4((a_pos + u_view.xy) / u_view.zw, 1/*might be wrong*/, 1);
    gl_Position = vec4(a_pos, 1/*might be wrong*/, 1);
    color = a_color;
    uv = a_uv;
}
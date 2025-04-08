#version 330 core

layout (location = 0) in vec2 a_pos;
layout (location = 1) in vec4 a_color;
layout (location = 2) in vec2 a_uv;

out vec4 color;
out vec2 uv;

uniform vec4 u_view; // x, y, width, height

void main() {
    gl_Position.xy = (a_pos - u_view.xy) / u_view.zw; // convert from view to 0..1
    gl_Position.y = 1 - gl_Position.y; // compatibility mode has this flipped for some reason
    gl_Position.xy = gl_Position.xy * 2 - 1; // 0..1 to -1..1
    gl_Position.zw = vec2(1);
    color = a_color;
    uv = a_uv;
}
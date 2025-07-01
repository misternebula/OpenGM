#version 330 core

layout (location = 0) in vec2 in_Position;
layout (location = 1) in vec4 in_Colour;
layout (location = 2) in vec2 in_TextureCoord;

out vec4 v_vColour;
out vec2 v_vTexcoord;

uniform vec4 u_view; // x, y, width, height
uniform bool u_flipY; // backbuffer drawing should be flipped. nothing else should

void main() {
    gl_Position.xy = (in_Position - u_view.xy) / u_view.zw; // convert from view to 0..1
    if (u_flipY) gl_Position.y = 1 - gl_Position.y;
    gl_Position.xy = gl_Position.xy * 2 - 1; // 0..1 to -1..1
    gl_Position.zw = vec2(1);
    v_vColour = in_Colour;
    v_vTexcoord = in_TextureCoord;
}
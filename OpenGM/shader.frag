#version 330 core

in vec4 color;
in vec2 uv;

// TODO: turn off texture sample, just use white
uniform sampler2D u_tex;

void main() {
    gl_FragColor = color * texture2D(u_tex, uv);
}
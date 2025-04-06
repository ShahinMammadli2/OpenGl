#verison 330 core;
out vec4 FragColor;

in vec2 texCoords;

uniform sampler2D texture;

void main()
{
    FragColor = texture(texture, texCoords);
}
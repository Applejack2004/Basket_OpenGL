#version 460 core

out vec4 FragColor;
in vec2 texCoord;

uniform sampler2D texture0;
uniform vec4 overrideColor = vec4(-1.0);

void main()
{
	if (overrideColor.r >= 0.0)
		FragColor = overrideColor;
	else
		FragColor = texture(texture0, texCoord);
}

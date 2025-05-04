#version 330 core
in VS_OUT {
    vec3 FragPos;
    vec3 Normal;
    vec2 TexCoords;
} fs_in;

out vec4 FragColor;

// you can swap this for a texture if you like
uniform sampler2D diffuseTexture;  

uniform vec3 lightPos;
uniform vec3 viewPos;

// the depth cubemap from your point-light pass
uniform samplerCube depthMap;
uniform float far_plane;

// little helper to sample your cube map shadows
float ShadowCalculation(vec3 fragPos)
{
    vec3 fragToLight = fragPos - lightPos;
    float currentDepth = length(fragToLight);

    // sample closest depth from cubemap (in [0,1])
    float closestDepth = texture(depthMap, fragToLight).r;
    // undo the [0,1] mapping
    closestDepth *= far_plane; 

    // simple bias to avoid self-shadowing
    float bias = 0.05;
    float shadow = currentDepth - bias > closestDepth ? 1.0 : 0.0;

    return shadow;
}

void main()
{
    // basic material color (could also be texture(diffuseTexture, fs_in.TexCoords).rgb)
    vec3 color   = texture(diffuseTexture, fs_in.TexCoords).rgb;
    vec3 normal  = normalize(fs_in.Normal);

    // ambient + diffuse (no specular here, but you can add it similarly)
    vec3 ambient = 0.1 * color;
    vec3 lightDir = normalize(lightPos - fs_in.FragPos);
    float diff = max(dot(lightDir, normal), 0.0);
    vec3 diffuse = diff * color;

    // compute the shadow factor
    float shadow = ShadowCalculation(fs_in.FragPos);

    // mix out the diffuse term where in shadow
    vec3 lighting = ambient + (1.0 - shadow) * diffuse;

    FragColor = vec4(lighting, 1.0);
}

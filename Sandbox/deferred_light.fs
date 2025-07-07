// deferred_light.fs
#version 330 core
out vec4 FragColor;

in vec2 TexCoords;

// 1) G-Buffer inputs
uniform sampler2D gPosition;      // RGBA16F: .rgb = view-space position
uniform sampler2D gNormal;        // RGBA16F: .rgb = view-space normal
uniform sampler2D gAlbedoSpec;    // RGBA:   .rgb = albedo, .a = specular intensity

// 2) Shadow map & camera
uniform samplerCube depthMap; 
uniform float       far_plane;
uniform vec3        viewPos;
//uniform samplerCube skybox;

// 3) Lights
uniform vec3        lightPos;     // single point light for now
uniform vec3        lightColor;
uniform bool        shadows;

// 4) PCF disk for shadows (same as your forward shader)
vec3 sampleOffsetDirections[20] = vec3[](
   vec3( 1,  1,  1), vec3( 1, -1,  1), vec3(-1, -1,  1), vec3(-1,  1,  1),
   vec3( 1,  1, -1), vec3( 1, -1, -1), vec3(-1, -1, -1), vec3(-1,  1, -1),
   vec3( 1,  1,  0), vec3( 1, -1,  0), vec3(-1, -1,  0), vec3(-1,  1,  0),
   vec3( 1,  0,  1), vec3(-1,  0,  1), vec3( 1,  0, -1), vec3(-1,  0, -1),
   vec3( 0,  1,  1), vec3( 0, -1,  1), vec3( 0, -1, -1), vec3( 0,  1, -1)
);


float ShadowCalculation(vec3 fragPos)
{
    vec3 fragToLight = fragPos - lightPos;
    float currentDepth = length(fragToLight);
    float bias = 0.15;
    float shadow = 0.0;
    int samples = 20;
    float viewDistance = length(viewPos - fragPos);
    float diskRadius = (1.0 + viewDistance / far_plane) / 25.0;

    for (int i = 0; i < samples; ++i) {
        float closestDepth = texture(depthMap, fragToLight + sampleOffsetDirections[i] * diskRadius).r;
        closestDepth *= far_plane;
        if (currentDepth - bias > closestDepth)
            shadow += 1.0;
    }
    shadow /= float(samples);
    return shadow;
}

void main()
{
    // — fetch G-Buffer —
    vec3 FragPos = texture(gPosition,   TexCoords).rgb;
    vec3 Normal  = normalize(texture(gNormal,    TexCoords).rgb);
    vec3 Albedo  = texture(gAlbedoSpec, TexCoords).rgb;
    float Spec   = texture(gAlbedoSpec, TexCoords).a;

    // — ambient —
    vec3 ambient = 0.1 * Albedo;

    // — diffuse & specular —
    vec3 lightDir   = normalize(lightPos - FragPos);
    float diff      = max(dot(lightDir, Normal), 0.0);
    vec3 diffuse    = diff * lightColor * Albedo;

    vec3 viewDir    = normalize(viewPos - FragPos);
    vec3 halfwayDir = normalize(lightDir + viewDir);
    float specF     = pow(max(dot(Normal, halfwayDir), 0.0), 64.0);
    vec3 specular   = specF * lightColor * Spec;

    // — attenuation —
    float distance  = length(lightPos - FragPos);
    float attenuation = 1.0 / (distance * distance);
    diffuse  *= attenuation;
    specular *= attenuation;

    // — shadows —
    float shadowVal = shadows ? ShadowCalculation(FragPos) : 0.0;

    vec3 lighting = ambient 
                   + (1.0 - shadowVal) * (diffuse + specular);

    FragColor = vec4(lighting, 1.0);
}

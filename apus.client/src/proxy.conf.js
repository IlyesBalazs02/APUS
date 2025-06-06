const { env } = require('process');

/*const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:7244` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:5084';*/
  const target = 'https://localhost:7244';

const PROXY_CONFIG = [
  {
    context: [
      "/api",
      "/routing",
      "/routing/**",
      "/activities",
      "/images/**",
      "/Images/**",
      "/Activities/**",
      "/Users/**"
    ],
    target,
    secure: false
  }
]

module.exports = PROXY_CONFIG;

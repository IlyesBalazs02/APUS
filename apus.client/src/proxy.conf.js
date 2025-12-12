const { env } = require('process');

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
      "profile/**",
      "Profile/**",
      "UserProfile/**",
      "siteUser/**",
      "SiteUser/**",
      "Account/**",
      "Account/**",
      "privacy/**",
      "Privacy/**",
      "Search/**",
      "Friends/**",
      "Routing/**",
      "GroupSettings/**",
      "GroupPost/**",
      "/Activities/**",
      "Auth/**",
      "/Users/**",
      "/Perm/**"
    ],
    target,
    secure: false
  }
]

module.exports = PROXY_CONFIG;

export interface RegisterDto { email: string; password: string; confirmPassword: string; }
export interface LoginDto { firstname: string; lastname: string; email: string; password: string; }
export interface JwtResponse { token: string; }

export interface RegisterDto { email: string; password: string; confirmPassword: string; }
export interface LoginDto { email: string; password: string; }
export interface JwtResponse { token: string; }

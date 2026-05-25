import type { AuthUser } from "@/types"

const TOKEN_KEY = "academia_token"
const USER_KEY = "academia_user"

// Decodifica el payload de un JWT (base64url) sin librerías externas
function decodeJwtPayload(token: string): Record<string, string> | null {
  try {
    const payload = token.split(".")[1]
    const decoded = atob(payload.replace(/-/g, "+").replace(/_/g, "/"))
    return JSON.parse(decoded)
  } catch {
    return null
  }
}

export function saveToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token)
}

export function getToken(): string | null {
  if (typeof window === "undefined") return null
  return localStorage.getItem(TOKEN_KEY)
}

export function removeToken(): void {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(USER_KEY)
}

export function saveUser(user: AuthUser): void {
  localStorage.setItem(USER_KEY, JSON.stringify(user))
}

export function getUser(): AuthUser | null {
  if (typeof window === "undefined") return null
  const raw = localStorage.getItem(USER_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as AuthUser
  } catch {
    return null
  }
}

// Extrae el ID del usuario desde el claim 'nameid' del JWT
export function getUserIdFromToken(token: string): string | null {
  const payload = decodeJwtPayload(token)
  return payload?.nameid ?? payload?.sub ?? null
}

export function isTokenExpired(token: string): boolean {
  const payload = decodeJwtPayload(token)
  if (!payload?.exp) return true
  return Date.now() / 1000 > Number(payload.exp)
}

export function isAuthenticated(): boolean {
  const token = getToken()
  if (!token) return false
  return !isTokenExpired(token)
}

const SESSION_EXPIRED_KEY = "academia_session_expired"

export function markSessionExpired(): void {
  if (typeof window === "undefined") return
  sessionStorage.setItem(SESSION_EXPIRED_KEY, "1")
}

export function consumeSessionExpiredFlag(): boolean {
  if (typeof window === "undefined") return false
  const had = sessionStorage.getItem(SESSION_EXPIRED_KEY) !== null
  if (had) sessionStorage.removeItem(SESSION_EXPIRED_KEY)
  return had
}

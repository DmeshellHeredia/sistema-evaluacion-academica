"use client"

import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  type ReactNode,
} from "react"
import { useRouter } from "next/navigation"
import type { AuthUser, LoginRequest } from "@/types"
import { authApi } from "@/lib/api"
import {
  saveToken,
  saveUser,
  getUser,
  removeToken,
  isAuthenticated,
  getUserIdFromToken,
} from "@/lib/auth"

interface AuthContextValue {
  user: AuthUser | null
  hydrated: boolean
  login: (data: LoginRequest) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

function getInitialUser(): AuthUser | null {
  if (typeof window === "undefined") return null
  if (!isAuthenticated()) {
    removeToken()
    return null
  }
  return getUser()
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(getInitialUser)
  const [hydrated, setHydrated] = useState(false)
  const router = useRouter()

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setHydrated(true)
  }, [])

  const login = useCallback(async (data: LoginRequest) => {
    const response = await authApi.login(data)

    const authUser: AuthUser = {
      id: getUserIdFromToken(response.token) ?? "",
      email: response.email,
      fullName: response.fullName,
      role: response.role as AuthUser["role"],
      expiresAt: response.expiresAt,
    }

    saveToken(response.token)
    saveUser(authUser)
    setUser(authUser)
    router.push("/dashboard")
  }, [router])

  const logout = useCallback(() => {
    removeToken()
    setUser(null)
    router.push("/login")
  }, [router])

  return (
    <AuthContext.Provider value={{ user, hydrated, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error("useAuth debe usarse dentro de AuthProvider")
  return ctx
}

import { render, screen, waitFor, fireEvent, act } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { vi, describe, it, expect, beforeEach } from "vitest"
import LoginPage from "@/app/login/page"
import { toast } from "sonner"

// vi.hoisted ensures these are available inside vi.mock factories (hoisted before imports)
const mockLogin = vi.hoisted(() => vi.fn())
const mockConsumeSessionExpiredFlag = vi.hoisted(() => vi.fn())

vi.mock("@/context/auth-context", () => ({
  useAuth: () => ({
    login: mockLogin,
    user: null,
    hydrated: true,
    logout: vi.fn(),
  }),
}))

vi.mock("@/lib/auth", () => ({
  consumeSessionExpiredFlag: mockConsumeSessionExpiredFlag,
  isAuthenticated: vi.fn(() => false),
  saveToken: vi.fn(),
  saveUser: vi.fn(),
  getUser: vi.fn(() => null),
  removeToken: vi.fn(),
  getUserIdFromToken: vi.fn(() => null),
}))

vi.mock("sonner", () => ({
  toast: {
    warning: vi.fn(),
    error: vi.fn(),
    success: vi.fn(),
  },
  Toaster: () => null,
}))

vi.mock("@/lib/api", () => ({
  getErrorMessage: vi.fn((err: unknown) =>
    err instanceof Error ? err.message : "Error desconocido"
  ),
  authApi: { login: vi.fn() },
}))

beforeEach(() => {
  vi.clearAllMocks()
  mockLogin.mockResolvedValue(undefined)
  mockConsumeSessionExpiredFlag.mockReturnValue(false)
})

describe("LoginPage — form rendering", () => {
  it("renders email and password inputs", () => {
    render(<LoginPage />)
    expect(screen.getByLabelText(/correo electrónico/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/contraseña/i, { selector: "input" })).toBeInTheDocument()
  })

  it("renders submit button with Ingresar label", () => {
    render(<LoginPage />)
    expect(screen.getByRole("button", { name: /ingresar/i })).toBeInTheDocument()
  })
})

describe("LoginPage — validation", () => {
  // jsdom enforces type="email" native validation before the submit event fires,
  // so react-hook-form / zod never run. fireEvent.submit bypasses that.
  it("shows error for an invalid email", async () => {
    const user = userEvent.setup()
    const { container } = render(<LoginPage />)

    await user.type(screen.getByLabelText(/correo electrónico/i), "not-an-email")
    await user.type(screen.getByLabelText(/contraseña/i, { selector: "input" }),"pass123")
    await act(async () => { fireEvent.submit(container.querySelector("form")!) })

    await screen.findByText("Correo electrónico inválido")
  })

  it("shows error when password is empty", async () => {
    const user = userEvent.setup()
    const { container } = render(<LoginPage />)

    await user.type(screen.getByLabelText(/correo electrónico/i), "a@b.com")
    await act(async () => { fireEvent.submit(container.querySelector("form")!) })

    await screen.findByText("La contraseña es requerida")
  })
})

describe("LoginPage — submission", () => {
  it("calls login with typed credentials", async () => {
    const user = userEvent.setup()
    render(<LoginPage />)

    await user.type(screen.getByLabelText(/correo electrónico/i), "admin@academia.com")
    await user.type(screen.getByLabelText(/contraseña/i, { selector: "input" }),"Admin123!")
    await user.click(screen.getByRole("button", { name: /ingresar/i }))

    await waitFor(() =>
      expect(mockLogin).toHaveBeenCalledWith({
        email: "admin@academia.com",
        password: "Admin123!",
      })
    )
  })

  it("shows error toast when login throws", async () => {
    const user = userEvent.setup()
    mockLogin.mockRejectedValueOnce(new Error("Credenciales inválidas"))

    render(<LoginPage />)
    await user.type(screen.getByLabelText(/correo electrónico/i), "bad@email.com")
    await user.type(screen.getByLabelText(/contraseña/i, { selector: "input" }),"wrongpass")
    await user.click(screen.getByRole("button", { name: /ingresar/i }))

    await waitFor(() => expect(toast.error).toHaveBeenCalledWith("Credenciales inválidas"))
  })

  it("disables submit button while login is in progress", async () => {
    const user = userEvent.setup()
    let resolveLogin!: () => void
    mockLogin.mockImplementationOnce(
      () => new Promise<void>((resolve) => { resolveLogin = resolve })
    )

    render(<LoginPage />)
    await user.type(screen.getByLabelText(/correo electrónico/i), "admin@academia.com")
    await user.type(screen.getByLabelText(/contraseña/i, { selector: "input" }),"Admin123!")

    // Don't await — check state while login is still pending
    const clickDone = user.click(screen.getByRole("button", { name: /ingresar/i }))

    await screen.findByText("Ingresando...")
    expect(screen.getByRole("button", { name: /ingresando/i })).toBeDisabled()

    resolveLogin()
    await clickDone
  })
})

describe("LoginPage — accessibility", () => {
  it("show/hide password button has an aria-label", () => {
    render(<LoginPage />)
    expect(
      screen.getByRole("button", { name: /mostrar contraseña/i })
    ).toBeInTheDocument()
  })

  it("toggles aria-label when password visibility is toggled", async () => {
    const user = userEvent.setup()
    render(<LoginPage />)

    const toggle = screen.getByRole("button", { name: /mostrar contraseña/i })
    await user.click(toggle)
    expect(
      screen.getByRole("button", { name: /ocultar contraseña/i })
    ).toBeInTheDocument()
  })
})

describe("LoginPage — session expiry", () => {
  it("shows a warning toast when session had expired", () => {
    mockConsumeSessionExpiredFlag.mockReturnValue(true)
    render(<LoginPage />)
    expect(toast.warning).toHaveBeenCalledWith(
      "Tu sesión expiró. Inicia sesión nuevamente."
    )
  })

  it("does not show warning when session is fresh", () => {
    mockConsumeSessionExpiredFlag.mockReturnValue(false)
    render(<LoginPage />)
    expect(toast.warning).not.toHaveBeenCalled()
  })
})

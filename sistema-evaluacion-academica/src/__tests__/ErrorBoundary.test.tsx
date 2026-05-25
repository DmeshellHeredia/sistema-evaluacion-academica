import { render, screen, fireEvent } from "@testing-library/react"
import { vi, describe, it, expect, beforeEach, afterEach } from "vitest"
import { ErrorBoundary } from "@/components/error-boundary"

function Bomb({ shouldThrow }: { shouldThrow: boolean }) {
  if (shouldThrow) throw new Error("Test explosion")
  return <p>Rendered fine</p>
}

beforeEach(() => {
  vi.spyOn(console, "error").mockImplementation(() => {})
})

afterEach(() => {
  vi.restoreAllMocks()
})

describe("ErrorBoundary — normal rendering", () => {
  it("renders children when no error is thrown", () => {
    render(
      <ErrorBoundary>
        <Bomb shouldThrow={false} />
      </ErrorBoundary>
    )
    expect(screen.getByText("Rendered fine")).toBeInTheDocument()
  })
})

describe("ErrorBoundary — error state", () => {
  it("shows fallback UI when a child throws", () => {
    render(
      <ErrorBoundary>
        <Bomb shouldThrow={true} />
      </ErrorBoundary>
    )
    expect(screen.getByRole("alert")).toBeInTheDocument()
    expect(screen.getByText("Algo salió mal")).toBeInTheDocument()
  })

  it("renders custom fallback when provided", () => {
    render(
      <ErrorBoundary fallback={<p>Custom fallback</p>}>
        <Bomb shouldThrow={true} />
      </ErrorBoundary>
    )
    expect(screen.getByText("Custom fallback")).toBeInTheDocument()
  })

  it("shows retry and reload buttons in default fallback", () => {
    render(
      <ErrorBoundary>
        <Bomb shouldThrow={true} />
      </ErrorBoundary>
    )
    expect(screen.getByRole("button", { name: /intentar de nuevo/i })).toBeInTheDocument()
    expect(screen.getByRole("button", { name: /recargar página/i })).toBeInTheDocument()
  })

  it("resets error state when retry button is clicked", () => {
    let shouldThrow = true
    function ControlledBomb() {
      if (shouldThrow) throw new Error("Test explosion")
      return <p>Rendered fine</p>
    }

    render(
      <ErrorBoundary>
        <ControlledBomb />
      </ErrorBoundary>
    )

    expect(screen.getByRole("alert")).toBeInTheDocument()

    shouldThrow = false
    fireEvent.click(screen.getByRole("button", { name: /intentar de nuevo/i }))

    expect(screen.getByText("Rendered fine")).toBeInTheDocument()
    expect(screen.queryByRole("alert")).not.toBeInTheDocument()
  })
})

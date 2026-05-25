import { render, screen } from "@testing-library/react"
import { describe, it, expect } from "vitest"
import { GradeBadge } from "@/components/grade-badge"

describe("GradeBadge", () => {
  it("renders the category name", () => {
    render(<GradeBadge category="Excelente" />)
    expect(screen.getByText("Excelente")).toBeInTheDocument()
  })

  it("renders value formatted to one decimal when provided", () => {
    render(<GradeBadge category="Buena" value={8.5} />)
    expect(screen.getByText("8.5")).toBeInTheDocument()
  })

  it("rounds value correctly with toFixed(1)", () => {
    render(<GradeBadge category="Excelente" value={9.0} />)
    expect(screen.getByText("9.0")).toBeInTheDocument()
  })

  it("does not render a numeric span when value is omitted", () => {
    render(<GradeBadge category="Buena" />)
    expect(screen.queryByText(/^\d+\.\d$/)).not.toBeInTheDocument()
  })

  it("applies emerald background for Excelente", () => {
    const { container } = render(<GradeBadge category="Excelente" />)
    expect(container.firstChild).toHaveClass("bg-emerald-100")
  })

  it("applies blue background for Buena", () => {
    const { container } = render(<GradeBadge category="Buena" />)
    expect(container.firstChild).toHaveClass("bg-blue-100")
  })

  it("applies red background for Por mejorar", () => {
    const { container } = render(<GradeBadge category="Por mejorar" />)
    expect(container.firstChild).toHaveClass("bg-red-100")
  })

  it("renders without crashing for an unknown category", () => {
    render(<GradeBadge category="Desconocida" />)
    expect(screen.getByText("Desconocida")).toBeInTheDocument()
  })

  it("uses sm padding class when size='sm'", () => {
    const { container } = render(<GradeBadge category="Excelente" size="sm" />)
    expect(container.firstChild).toHaveClass("px-2")
  })
})

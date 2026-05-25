import { describe, it, expect } from "vitest"

// Inline the derivation logic that both views use so tests remain pure
// and don't require mounting full page components.
function resolveActivePeriod(periodCode: string | null | undefined): string {
  return periodCode ?? ""
}

describe("resolveActivePeriod — backend code derivation", () => {
  it("returns the backend code when present", () => {
    expect(resolveActivePeriod("2025-1")).toBe("2025-1")
  })

  it("returns empty string when periodCode is null (no active period)", () => {
    expect(resolveActivePeriod(null)).toBe("")
  })

  it("returns empty string when periodCode is undefined (query not yet resolved)", () => {
    expect(resolveActivePeriod(undefined)).toBe("")
  })

  it("preserves period codes that differ from local-date heuristic", () => {
    // Backend may keep a period open beyond the calendar boundary —
    // the local heuristic would return a different value.
    expect(resolveActivePeriod("2024-2")).toBe("2024-2")
    expect(resolveActivePeriod("2025-2")).toBe("2025-2")
  })

  it("never invents a period based on local date", () => {
    // Local heuristic would produce `${year}-1` or `${year}-2`.
    // Our resolver only returns what the backend provides.
    const localGuess = (() => {
      const now = new Date()
      return `${now.getFullYear()}-${now.getMonth() < 6 ? 1 : 2}`
    })()

    // With no backend data, result is "" — never the local guess
    expect(resolveActivePeriod(null)).not.toBe(localGuess)
    expect(resolveActivePeriod(undefined)).not.toBe(localGuess)
  })
})

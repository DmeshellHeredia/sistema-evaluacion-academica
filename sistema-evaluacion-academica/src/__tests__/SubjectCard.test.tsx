import { render, screen } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { vi, describe, it, expect, beforeEach } from "vitest"
import { SubjectCard } from "@/app/dashboard/catalog/page"
import type { SubjectCatalogItemDto, SectionSummaryDto } from "@/types"

// Break the auth-context → next/navigation import chain
vi.mock("@/context/auth-context", () => ({
  useAuth: vi.fn(() => ({ user: null, hydrated: true, login: vi.fn(), logout: vi.fn() })),
}))

vi.mock("@tanstack/react-query", () => ({
  useQuery: vi.fn(),
  useMutation: vi.fn(),
  useQueryClient: vi.fn(),
}))

vi.mock("sonner", () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}))

vi.mock("@/lib/api", () => ({
  studentsApi: { me: vi.fn() },
  enrollmentsApi: { getMyCatalog: vi.fn(), getMySchedule: vi.fn(), enroll: vi.fn(), unenroll: vi.fn() },
  periodsApi: { getStatus: vi.fn() },
  getErrorMessage: vi.fn(),
}))

function makeSection(overrides: Partial<SectionSummaryDto> = {}): SectionSummaryDto {
  return {
    sectionId: "sec1",
    sectionCode: "A",
    professorName: "Prof. García",
    dayOfWeek: "Lunes",
    startTime: "08:00",
    endTime: "10:00",
    modality: "Presencial",
    capacity: 30,
    enrolledCount: 10,
    isFull: false,
    classroom: "Aula 101",
    ...overrides,
  }
}

function makeItem(overrides: Partial<SubjectCatalogItemDto> = {}): SubjectCatalogItemDto {
  return {
    subjectId: "sub1",
    code: "MAT101",
    name: "Matemáticas I",
    description: "Curso de álgebra",
    credits: 4,
    semesterLevel: 1,
    availableSections: [makeSection()],
    status: "Disponible",
    grade: null,
    period: null,
    missingPrerequisites: [],
    hasScheduleConflict: false,
    conflictingWith: null,
    ...overrides,
  }
}

const defaultProps = {
  loading: false,
  expanded: false,
  selectedSection: "sec1",
  enrolledSectionId: undefined as string | undefined,
  enrollmentIsOpen: true,
  onSelectSection: vi.fn(),
  onToggleExpand: vi.fn(),
  onEnroll: vi.fn(),
  onUnenroll: vi.fn(),
}

beforeEach(() => vi.clearAllMocks())

describe("SubjectCard — display", () => {
  it("renders subject name and code", () => {
    render(<SubjectCard item={makeItem()} {...defaultProps} />)
    expect(screen.getByText("Matemáticas I")).toBeInTheDocument()
    expect(screen.getByText(/MAT101/)).toBeInTheDocument()
  })

  it("shows Disponible status badge", () => {
    render(<SubjectCard item={makeItem({ status: "Disponible" })} {...defaultProps} />)
    expect(screen.getByText("Disponible")).toBeInTheDocument()
  })

  it("shows Bloqueada status badge", () => {
    render(<SubjectCard item={makeItem({ status: "Bloqueada" })} {...defaultProps} />)
    expect(screen.getByText("Bloqueada")).toBeInTheDocument()
  })
})

describe("SubjectCard — enroll button", () => {
  it("shows Inscribirse button for Disponible subject with open enrollment", () => {
    render(<SubjectCard item={makeItem({ status: "Disponible" })} {...defaultProps} />)
    expect(screen.getByRole("button", { name: /inscribirse/i })).toBeInTheDocument()
  })

  it("does not show Inscribirse button for Bloqueada subject", () => {
    render(<SubjectCard item={makeItem({ status: "Bloqueada" })} {...defaultProps} />)
    expect(screen.queryByRole("button", { name: /inscribirse/i })).not.toBeInTheDocument()
  })

  it("shows 'Selección cerrada' when enrollment period is closed", () => {
    render(
      <SubjectCard
        item={makeItem({ status: "Disponible" })}
        {...defaultProps}
        enrollmentIsOpen={false}
      />
    )
    expect(screen.getByText("Selección cerrada")).toBeInTheDocument()
  })

  it("shows 'Sección llena' when selected section is full", () => {
    render(
      <SubjectCard
        item={makeItem({
          status: "Disponible",
          availableSections: [makeSection({ isFull: true })],
        })}
        {...defaultProps}
        selectedSection="sec1"
      />
    )
    expect(screen.getByText("Sección llena")).toBeInTheDocument()
  })

  it("calls onEnroll with section id when Inscribirse is clicked", async () => {
    const user = userEvent.setup()
    const onEnroll = vi.fn()
    render(
      <SubjectCard item={makeItem({ status: "Disponible" })} {...defaultProps} onEnroll={onEnroll} />
    )
    await user.click(screen.getByRole("button", { name: /inscribirse/i }))
    expect(onEnroll).toHaveBeenCalledWith("sec1")
  })
})

describe("SubjectCard — unenroll button", () => {
  it("shows Cancelar inscripción for Inscrita subject when enrollment is open", () => {
    render(
      <SubjectCard
        item={makeItem({ status: "Inscrita" })}
        {...defaultProps}
        enrolledSectionId="sec1"
        enrollmentIsOpen={true}
      />
    )
    expect(screen.getByRole("button", { name: /cancelar inscripción/i })).toBeInTheDocument()
  })
})

describe("SubjectCard — prerequisites", () => {
  it("shows missing prerequisites section when prerequisites are unmet", () => {
    render(
      <SubjectCard
        item={makeItem({
          status: "Bloqueada",
          missingPrerequisites: [
            { subjectId: "pre1", code: "ALG100", name: "Álgebra Básica", yourGrade: null },
          ],
        })}
        {...defaultProps}
      />
    )
    expect(screen.getByText("Prerequisitos pendientes")).toBeInTheDocument()
    expect(screen.getByText(/ALG100/)).toBeInTheDocument()
  })

  it("does not show prerequisites section when all are met", () => {
    render(<SubjectCard item={makeItem()} {...defaultProps} />)
    expect(screen.queryByText("Prerequisitos pendientes")).not.toBeInTheDocument()
  })
})

describe("SubjectCard — schedule conflict", () => {
  it("shows Choque de horario for Disponible subject with conflict", () => {
    render(
      <SubjectCard
        item={makeItem({
          status: "Disponible",
          hasScheduleConflict: true,
          conflictingWith: "Física I",
        })}
        {...defaultProps}
      />
    )
    expect(screen.getByText("Choque de horario")).toBeInTheDocument()
    expect(screen.getByText("Física I")).toBeInTheDocument()
  })

  it("does not show Choque de horario for Inscrita subject even with conflict flag", () => {
    render(
      <SubjectCard
        item={makeItem({ status: "Inscrita", hasScheduleConflict: true })}
        {...defaultProps}
        enrolledSectionId="sec1"
      />
    )
    expect(screen.queryByText("Choque de horario")).not.toBeInTheDocument()
  })
})

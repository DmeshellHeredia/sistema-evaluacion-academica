"use client"

import { useState, useMemo } from "react"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import {
  BookOpen, CheckCircle, Clock, Lock, AlertCircle, Loader2,
  GraduationCap, CalendarDays, TriangleAlert, Users, MapPin, ChevronDown,
} from "lucide-react"
import { toast } from "sonner"
import { useAuth } from "@/context/auth-context"
import { studentsApi, enrollmentsApi, periodsApi } from "@/lib/api"
import { onMutationError } from "@/lib/mutation-error"
import { ErrorState } from "@/components/error-state"
import type { SubjectCatalogItemDto, SectionSummaryDto, EnrollmentStatus } from "@/types"
import { DAY_LABELS } from "@/types"

const STATUS_CONFIG: Record<
  EnrollmentStatus,
  { label: string; icon: React.ElementType; color: string; bg: string; border: string; canEnroll: boolean; canUnenroll: boolean }
> = {
  Disponible:    { label: "Disponible",      icon: BookOpen,    color: "text-blue-500",         bg: "bg-blue-500/10",    border: "border-blue-500/20",    canEnroll: true,  canUnenroll: false },
  Inscrita:      { label: "Inscrita",        icon: Clock,       color: "text-amber-500",        bg: "bg-amber-500/10",   border: "border-amber-500/20",   canEnroll: false, canUnenroll: true  },
  Aprobada:      { label: "Aprobada",        icon: CheckCircle, color: "text-green-500",        bg: "bg-green-500/10",   border: "border-green-500/20",   canEnroll: false, canUnenroll: false },
  Reprobada:     { label: "Reprobada",       icon: AlertCircle, color: "text-red-500",          bg: "bg-red-500/10",     border: "border-red-500/20",     canEnroll: true,  canUnenroll: false },
  Bloqueada:     { label: "Bloqueada",       icon: Lock,        color: "text-muted-foreground", bg: "bg-muted/50",       border: "border-border",         canEnroll: false, canUnenroll: false },
  SemestrePrevio:{ label: "Sem. siguiente",  icon: Lock,        color: "text-muted-foreground", bg: "bg-muted/30",       border: "border-border",         canEnroll: false, canUnenroll: false },
}

export default function CatalogPage() {
  const { user } = useAuth()
  const qc = useQueryClient()
  const [filter, setFilter] = useState<EnrollmentStatus | "Todas">("Todas")
  const [expandedSubject, setExpandedSubject] = useState<string | null>(null)

  const { data: studentData, isLoading: studentLoading, isError: studentError } = useQuery({
    queryKey: ["students", "me"],
    queryFn: studentsApi.me,
    enabled: !!user,
  })

  const { data: enrollmentStatus } = useQuery({
    queryKey: ["enrollment-status"],
    queryFn: periodsApi.getStatus,
    enabled: !!user,
  })

  const {
    data: catalog,
    isLoading: catalogLoading,
    isError: isCatalogError,
    error: catalogError,
    refetch,
  } = useQuery({
    queryKey: ["catalog", "me"],
    queryFn: enrollmentsApi.getMyCatalog,
    enabled: !!studentData,
  })

  const { data: schedule } = useQuery({
    queryKey: ["my-schedule"],
    queryFn: enrollmentsApi.getMySchedule,
    enabled: !!studentData,
  })

  const enrolledSectionMap = useMemo(() => {
    const map: Record<string, string> = {}
    for (const s of schedule?.sections ?? []) {
      map[s.subjectId] = s.sectionId
    }
    return map
  }, [schedule])

  const [pendingSubjectId, setPendingSubjectId] = useState<string | null>(null)
  const [selectedSections, setSelectedSections] = useState<Record<string, string>>({})

  const enrollMutation = useMutation({
    mutationFn: ({ sectionId }: { subjectId: string; sectionId: string }) =>
      enrollmentsApi.enroll({ studentId: studentData!.id, sectionId }),
    onMutate: ({ subjectId }) => setPendingSubjectId(subjectId),
    onSettled: () => setPendingSubjectId(null),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["catalog", "me"] })
      qc.invalidateQueries({ queryKey: ["my-schedule"] })
      toast.success("Inscripción realizada")
    },
    onError: onMutationError,
  })

  const unenrollMutation = useMutation({
    mutationFn: ({ sectionId }: { subjectId: string; sectionId: string }) =>
      enrollmentsApi.unenroll(studentData!.id, sectionId),
    onMutate: ({ subjectId }) => setPendingSubjectId(subjectId),
    onSettled: () => setPendingSubjectId(null),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["catalog", "me"] })
      qc.invalidateQueries({ queryKey: ["my-schedule"] })
      toast.success("Inscripción cancelada")
    },
    onError: onMutationError,
  })

  const filterOptions: Array<EnrollmentStatus | "Todas"> = [
    "Todas", "Disponible", "Inscrita", "Aprobada", "Reprobada", "Bloqueada",
  ]

  const isLoading = studentLoading || catalogLoading

  if (studentError || isCatalogError) {
    return <ErrorState error={catalogError ?? new Error("No se pudo cargar el perfil")} onRetry={() => void refetch()} />
  }

  const filtered = filter === "Todas" ? catalog ?? [] : (catalog ?? []).filter((i) => i.status === filter)
  const grouped = groupBySemester(filtered)

  function getSectionId(item: SubjectCatalogItemDto) {
    return selectedSections[item.subjectId] ?? item.availableSections[0]?.sectionId ?? ""
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center gap-3">
        <div className="flex-1">
          <h1 className="text-lg font-bold text-foreground">Mis Materias</h1>
          {studentData && (
            <p className="text-sm text-muted-foreground mt-0.5">
              {studentData.career} · Semestre {studentData.semester}
            </p>
          )}
        </div>
        <div className="flex flex-wrap gap-1.5">
          {filterOptions.map((f) => (
            <button
              key={f}
              onClick={() => setFilter(f)}
              className={`px-3 py-1 rounded-full text-xs font-medium transition-colors cursor-pointer ${
                filter === f ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground hover:bg-muted/80"
              }`}
            >
              {f === "Todas" ? "Todas" : STATUS_CONFIG[f as EnrollmentStatus].label}
              {f !== "Todas" && catalog && (
                <span className="ml-1 opacity-70">({catalog.filter((i) => i.status === f).length})</span>
              )}
            </button>
          ))}
        </div>
      </div>

      {enrollmentStatus && !enrollmentStatus.isOpen && (
        <div className="flex items-start gap-3 px-4 py-3 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-700 dark:text-amber-400">
          <Lock className="h-4 w-4 shrink-0 mt-0.5" />
          <div>
            <p className="text-sm font-semibold">La selección de materias no está activa</p>
            <p className="text-xs mt-0.5 opacity-80">Contacta a la administración para saber cuándo abrirá el próximo período.</p>
          </div>
        </div>
      )}
      {enrollmentStatus?.isOpen && (
        <div className="flex items-center gap-3 px-4 py-3 rounded-xl bg-green-500/10 border border-green-500/20 text-green-700 dark:text-green-400">
          <CalendarDays className="h-4 w-4 shrink-0" />
          <p className="text-sm font-medium">
            Selección activa — <span className="font-bold">{enrollmentStatus.periodName}</span>
            {enrollmentStatus.endDate && (
              <span className="ml-1 opacity-80 text-xs">
                · hasta {new Date(enrollmentStatus.endDate).toLocaleDateString("es-ES", { day: "numeric", month: "long" })}
              </span>
            )}
          </p>
        </div>
      )}

      {isLoading ? (
        <div className="space-y-6">
          {[1, 2, 3].map((s) => (
            <div key={s}>
              <div className="h-5 w-28 bg-muted animate-pulse rounded mb-3" />
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                {[1, 2, 3].map((i) => (
                  <div key={i} className="bg-card border border-border rounded-2xl p-4 space-y-2 h-36">
                    <div className="h-4 bg-muted animate-pulse rounded w-3/4" />
                    <div className="h-3 bg-muted animate-pulse rounded w-1/2" />
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      ) : Object.keys(grouped).length === 0 ? (
        <div className="bg-card border border-border rounded-2xl p-14 text-center">
          <GraduationCap className="h-8 w-8 text-muted-foreground mx-auto mb-3" />
          <p className="text-sm text-muted-foreground">No hay materias para mostrar</p>
        </div>
      ) : (
        <div className="space-y-8">
          {Object.entries(grouped).map(([sem, items]) => (
            <div key={sem}>
              <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide mb-3">
                Semestre {sem}
              </h2>
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                {items.map((item) => (
                  <SubjectCard
                    key={item.subjectId}
                    item={item}
                    loading={pendingSubjectId === item.subjectId}
                    expanded={expandedSubject === item.subjectId}
                    selectedSection={getSectionId(item)}
                    enrolledSectionId={enrolledSectionMap[item.subjectId]}
                    enrollmentIsOpen={enrollmentStatus?.isOpen ?? true}
                    onSelectSection={(id) => setSelectedSections((p) => ({ ...p, [item.subjectId]: id }))}
                    onToggleExpand={() => setExpandedSubject(expandedSubject === item.subjectId ? null : item.subjectId)}
                    onEnroll={(sectionId) => enrollMutation.mutate({ subjectId: item.subjectId, sectionId })}
                    onUnenroll={(sectionId) => unenrollMutation.mutate({ subjectId: item.subjectId, sectionId })}
                  />
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

export function SubjectCard({
  item,
  loading,
  expanded,
  selectedSection,
  enrolledSectionId,
  enrollmentIsOpen,
  onSelectSection,
  onToggleExpand,
  onEnroll,
  onUnenroll,
}: {
  item: SubjectCatalogItemDto
  loading: boolean
  expanded: boolean
  selectedSection: string
  enrolledSectionId: string | undefined
  enrollmentIsOpen: boolean
  onSelectSection: (id: string) => void
  onToggleExpand: () => void
  onEnroll: (sectionId: string) => void
  onUnenroll: (sectionId: string) => void
}) {
  const cfg = STATUS_CONFIG[item.status] ?? STATUS_CONFIG["Disponible"]
  const Icon = cfg.icon
  const sections = item.availableSections ?? []

  const enrolledSection = sections.find((s) => s.sectionId === enrolledSectionId) ?? sections[0]

  const selectedSectionObj = sections.find((s) => s.sectionId === selectedSection) ?? sections[0]
  const isConflict = item.hasScheduleConflict

  return (
    <div className={`bg-card border rounded-2xl flex flex-col gap-0 hover:shadow-sm transition-shadow overflow-hidden ${
      isConflict && item.status === "Disponible" ? "border-amber-500/40" : "border-border"
    }`}>
      <div className="p-4 flex flex-col gap-3">
        <div className="flex items-start justify-between gap-2">
          <div className="flex-1 min-w-0">
            <p className="font-semibold text-foreground text-sm leading-snug">{item.name}</p>
            <p className="text-xs text-muted-foreground mt-0.5">
              {item.code} · {item.credits} créd. · Sem. {item.semesterLevel}
            </p>
          </div>
          <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium shrink-0 ${cfg.bg} ${cfg.color}`}>
            <Icon className="h-3 w-3" />
            {cfg.label}
          </span>
        </div>

        {item.grade !== null && (
          <p className="text-xs text-muted-foreground">
            Nota: <span className={`font-semibold ${item.grade >= 7 ? "text-green-500" : "text-red-500"}`}>{item.grade.toFixed(1)}</span>
            {item.period && <span className="ml-1">({item.period})</span>}
          </p>
        )}

        {isConflict && item.status !== "Inscrita" && item.status !== "Aprobada" && (
          <div className="flex items-start gap-1.5 rounded-lg bg-amber-500/10 border border-amber-500/20 px-2.5 py-2">
            <TriangleAlert className="h-3.5 w-3.5 text-amber-500 shrink-0 mt-0.5" />
            <div>
              <p className="text-xs font-semibold text-amber-700 dark:text-amber-400">Choque de horario</p>
              {item.conflictingWith && (
                <p className="text-xs text-amber-600 dark:text-amber-500 mt-0.5 leading-tight">{item.conflictingWith}</p>
              )}
            </div>
          </div>
        )}

        {item.missingPrerequisites.length > 0 && (
          <div className="rounded-lg bg-red-500/5 border border-red-500/15 px-2.5 py-2 space-y-0.5">
            <p className="text-xs font-semibold text-red-600 dark:text-red-400">Prerequisitos pendientes</p>
            {item.missingPrerequisites.map((p) => (
              <p key={p.subjectId} className="text-xs text-muted-foreground flex items-center gap-1">
                <span className="text-red-400">·</span>
                {p.code} — {p.name}
                {p.yourGrade !== null && (
                  <span className="text-red-400 ml-1">(nota: {p.yourGrade.toFixed(1)})</span>
                )}
              </p>
            ))}
          </div>
        )}

        {sections.length > 0 && (
          <button
            onClick={onToggleExpand}
            className="flex items-center justify-between w-full text-xs text-muted-foreground hover:text-foreground transition-colors cursor-pointer py-0.5"
          >
            <span>{sections.length} sección{sections.length !== 1 ? "es" : ""} disponible{sections.length !== 1 ? "s" : ""}</span>
            <ChevronDown className={`h-3.5 w-3.5 transition-transform ${expanded ? "rotate-180" : ""}`} />
          </button>
        )}
      </div>

      {expanded && sections.length > 0 && (
        <div className="border-t border-border">
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead>
                <tr className="bg-muted/40">
                  <th className="text-left px-3 py-2 text-muted-foreground font-medium">Sec.</th>
                  <th className="text-left px-3 py-2 text-muted-foreground font-medium">Profesor</th>
                  <th className="text-left px-3 py-2 text-muted-foreground font-medium">Día / Hora</th>
                  <th className="text-left px-3 py-2 text-muted-foreground font-medium">Aula</th>
                  <th className="text-center px-3 py-2 text-muted-foreground font-medium">Cupo</th>
                  {cfg.canEnroll && <th className="px-2 py-2" />}
                </tr>
              </thead>
              <tbody>
                {sections.map((s) => (
                  <SectionRow
                    key={s.sectionId}
                    section={s}
                    canEnroll={cfg.canEnroll}
                    selected={selectedSection === s.sectionId}
                    onSelect={() => onSelectSection(s.sectionId)}
                    onEnroll={() => onEnroll(s.sectionId)}
                    loading={loading}
                  />
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {(cfg.canEnroll || cfg.canUnenroll) && (
        <div className="px-4 pb-4 pt-3 border-t border-border mt-auto">
          {cfg.canEnroll && (
            <button
              onClick={() => onEnroll(selectedSection)}
              disabled={loading || !selectedSection || selectedSectionObj?.isFull || !enrollmentIsOpen}
              className="w-full h-8 rounded-lg bg-primary text-primary-foreground text-xs font-semibold hover:bg-primary/90 disabled:opacity-50 transition-colors cursor-pointer flex items-center justify-center gap-1.5"
            >
              {loading && <Loader2 className="h-3 w-3 animate-spin" />}
              {!enrollmentIsOpen ? "Selección cerrada" : selectedSectionObj?.isFull ? "Sección llena" : "Inscribirse"}
            </button>
          )}
          {cfg.canUnenroll && enrolledSection && (
            <button
              onClick={() => onUnenroll(enrolledSection.sectionId)}
              disabled={loading || !enrollmentIsOpen}
              className="w-full h-8 rounded-lg border border-border text-xs font-medium text-muted-foreground hover:bg-muted disabled:opacity-50 transition-colors cursor-pointer flex items-center justify-center gap-1.5"
            >
              {loading && <Loader2 className="h-3 w-3 animate-spin" />}
              {!enrollmentIsOpen ? "Selección cerrada" : "Cancelar inscripción"}
            </button>
          )}
        </div>
      )}
    </div>
  )
}

function SectionRow({
  section,
  canEnroll,
  selected,
  onSelect,
  onEnroll,
  loading,
}: {
  section: SectionSummaryDto
  canEnroll: boolean
  selected: boolean
  onSelect: () => void
  onEnroll: () => void
  loading: boolean
}) {
  const occupancy = section.capacity > 0 ? Math.round((section.enrolledCount / section.capacity) * 100) : 0

  return (
    <tr
      className={`border-b border-border/50 transition-colors ${
        canEnroll ? "cursor-pointer hover:bg-muted/30" : ""
      } ${selected && canEnroll ? "bg-primary/5" : ""}`}
      onClick={canEnroll ? onSelect : undefined}
    >
      <td className="px-3 py-2">
        <div className="flex items-center gap-1.5">
          {canEnroll && (
            <div className={`h-3 w-3 rounded-full border-2 shrink-0 ${
              selected ? "border-primary bg-primary" : "border-border"
            }`} />
          )}
          <span className="font-mono font-semibold text-foreground">{section.sectionCode}</span>
        </div>
      </td>
      <td className="px-3 py-2 text-muted-foreground max-w-25 truncate">{section.professorName}</td>
      <td className="px-3 py-2 text-muted-foreground whitespace-nowrap">
        <p>{DAY_LABELS[section.dayOfWeek]}</p>
        <p className="text-muted-foreground/70">{section.startTime}–{section.endTime}</p>
      </td>
      <td className="px-3 py-2 text-muted-foreground">
        <div className="flex items-center gap-1">
          <MapPin className="h-3 w-3 shrink-0" />
          <span>{section.classroom || "—"}</span>
        </div>
      </td>
      <td className="px-3 py-2 text-center">
        <div className="flex flex-col items-center gap-0.5">
          <div className="flex items-center gap-1 text-muted-foreground">
            <Users className="h-3 w-3" />
            <span>{section.enrolledCount}/{section.capacity}</span>
          </div>
          <div className="w-12 h-1 rounded-full bg-muted overflow-hidden">
            <div
              className={`h-full rounded-full ${section.isFull ? "bg-red-500" : occupancy >= 70 ? "bg-amber-500" : "bg-emerald-500"}`}
              style={{ width: `${occupancy}%` }}
            />
          </div>
          {section.isFull && <span className="text-red-500 text-[10px] font-medium">Llena</span>}
        </div>
      </td>
      {canEnroll && (
        <td className="px-2 py-2">
          <button
            onClick={(e) => { e.stopPropagation(); onEnroll() }}
            disabled={loading || section.isFull}
            className="h-6 px-2 rounded-md bg-primary text-primary-foreground text-[10px] font-semibold disabled:opacity-40 hover:bg-primary/90 transition-colors cursor-pointer"
          >
            {loading ? <Loader2 className="h-3 w-3 animate-spin" /> : "Inscribir"}
          </button>
        </td>
      )}
    </tr>
  )
}

function groupBySemester(items: SubjectCatalogItemDto[]): Record<number, SubjectCatalogItemDto[]> {
  return items.reduce<Record<number, SubjectCatalogItemDto[]>>((acc, item) => {
    const sem = item.semesterLevel
    if (!acc[sem]) acc[sem] = []
    acc[sem].push(item)
    return acc
  }, {})
}

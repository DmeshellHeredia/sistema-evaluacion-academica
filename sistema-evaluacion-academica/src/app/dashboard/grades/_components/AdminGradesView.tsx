"use client"

import { useState } from "react"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import { Plus, Pencil, Trash2, Loader2, ChevronDown, ChevronLeft, ChevronRight } from "lucide-react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { toast } from "sonner"
import { studentsApi, sectionsApi, gradesApi, periodsApi } from "@/lib/api"
import { onMutationError } from "@/lib/mutation-error"
import { GradeBadge } from "@/components/grade-badge"
import { ErrorState } from "@/components/error-state"
import { Modal, Field, inputCls } from "@/components/modal"
import type { GradeDto } from "@/types"
import { DAY_LABELS } from "@/types"
import { EditDeleteModals } from "./EditDeleteModals"
import { createSchema, type CreateForm, type EditForm } from "./shared"

export function AdminGradesView() {
  const qc = useQueryClient()
  const [selectedStudentId, setSelectedStudentId] = useState("")
  const [page, setPage] = useState(1)
  const [modal, setModal] = useState<"create" | "edit" | "delete" | null>(null)
  const [selectedGrade, setSelectedGrade] = useState<GradeDto | null>(null)

  const { data: enrollmentStatus } = useQuery({
    queryKey: ["enrollment-status"],
    queryFn: periodsApi.getStatus,
  })

  const { data: studentsData } = useQuery({
    queryKey: ["students", 1, 100],
    queryFn: () => studentsApi.getAll(1, 100),
  })

  const { data: sections } = useQuery({
    queryKey: ["sections-lookup"],
    queryFn: () => sectionsApi.lookup(),
    staleTime: 60_000,
  })

  const {
    data: gradesData,
    isLoading: loadingGrades,
    isFetching,
    isError: gradesError,
    error: gradesErrorObj,
    refetch: refetchGrades,
  } = useQuery({
    queryKey: ["grades", "student", selectedStudentId, page],
    queryFn: () => gradesApi.getByStudent(selectedStudentId, page, 20),
    enabled: !!selectedStudentId,
  })

  const grades = gradesData?.items ?? []
  const totalPages = gradesData?.totalPages ?? 1

  const createMutation = useMutation({
    mutationFn: gradesApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["grades"] })
      qc.invalidateQueries({ queryKey: ["students"] })
      toast.success("Calificación registrada")
      setModal(null)
    },
    onError: onMutationError,
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: EditForm }) => gradesApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["grades"] })
      qc.invalidateQueries({ queryKey: ["students"] })
      toast.success("Calificación actualizada")
      setModal(null)
    },
    onError: onMutationError,
  })

  const deleteMutation = useMutation({
    mutationFn: gradesApi.delete,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["grades"] })
      qc.invalidateQueries({ queryKey: ["students"] })
      toast.success("Calificación eliminada")
      setModal(null)
      setSelectedGrade(null)
    },
    onError: onMutationError,
  })

  const createForm = useForm<CreateForm>({
    resolver: zodResolver(createSchema),
    defaultValues: { period: "" },
  })

  function openCreate() {
    createForm.reset({ studentId: selectedStudentId, sectionId: "", period: enrollmentStatus?.periodCode ?? "" })
    setModal("create")
  }

  function openEdit(grade: GradeDto) {
    setSelectedGrade(grade)
    setModal("edit")
  }

  function openDelete(grade: GradeDto) {
    setSelectedGrade(grade)
    setModal("delete")
  }

  const students = studentsData?.items ?? []
  const selectedStudent = students.find((s) => s.id === selectedStudentId)

  return (
    <div className="space-y-5">
      <div className="bg-card border border-border rounded-2xl p-5 shadow-sm">
        <h3 className="text-sm font-semibold text-foreground mb-3">Seleccionar estudiante</h3>
        <div className="flex flex-col sm:flex-row gap-3">
          <div className="relative flex-1 max-w-sm">
            <select
              value={selectedStudentId}
              onChange={(e) => { setSelectedStudentId(e.target.value); setPage(1) }}
              className={`${inputCls} appearance-none cursor-pointer pr-8`}
            >
              <option value="">— Elegir estudiante —</option>
              {students.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.studentCode} · {s.fullName}
                </option>
              ))}
            </select>
            <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
          </div>
          {selectedStudentId && (
            <button
              onClick={openCreate}
              className="flex items-center gap-2 h-9 px-4 rounded-lg bg-primary text-primary-foreground text-sm font-semibold hover:bg-primary/90 transition-colors cursor-pointer shadow-sm shadow-primary/20 shrink-0"
            >
              <Plus className="h-4 w-4" />
              Nueva calificación
            </button>
          )}
        </div>
      </div>

      {gradesError && selectedStudentId && (
        <ErrorState error={gradesErrorObj} onRetry={() => void refetchGrades()} />
      )}

      {!selectedStudentId ? (
        <div className="bg-card border border-border rounded-2xl p-14 text-center">
          <p className="text-sm text-muted-foreground">Selecciona un estudiante para ver sus calificaciones</p>
        </div>
      ) : gradesError && selectedStudentId ? null : (
        <div className="bg-card border border-border rounded-2xl shadow-sm overflow-hidden">
          {selectedStudent && (
            <div className="px-5 py-3.5 border-b border-border bg-muted/20 flex items-center justify-between">
              <div>
                <p className="text-sm font-semibold text-foreground">{selectedStudent.fullName}</p>
                <p className="text-xs text-muted-foreground">
                  {selectedStudent.career} · Semestre {selectedStudent.semester}
                </p>
              </div>
              {selectedStudent.overallAverage !== null && (
                <GradeBadge category={selectedStudent.averageCategory} value={selectedStudent.overallAverage} />
              )}
            </div>
          )}

          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border bg-muted/40">
                  <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">Materia</th>
                  <th className="text-center px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">Calificación</th>
                  <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide hidden sm:table-cell">Período</th>
                  <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide hidden lg:table-cell">Comentarios</th>
                  <th className="text-right px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {loadingGrades || isFetching ? (
                  Array.from({ length: 4 }).map((_, i) => (
                    <tr key={i} className="border-b border-border/50">
                      {[1, 2, 3, 4, 5].map((j) => (
                        <td key={j} className="px-4 py-3">
                          <div className="h-4 bg-muted animate-pulse rounded" />
                        </td>
                      ))}
                    </tr>
                  ))
                ) : grades.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="px-4 py-12 text-center text-muted-foreground text-sm">
                      {selectedStudentId ? "Este estudiante no tiene calificaciones registradas" : "Selecciona un estudiante"}
                    </td>
                  </tr>
                ) : (
                  grades.map((grade) => (
                    <tr key={grade.id} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                      <td className="px-4 py-3">
                        <p className="font-medium text-foreground">{grade.subjectName}</p>
                        <p className="text-xs text-muted-foreground">{grade.subjectCode}</p>
                      </td>
                      <td className="px-4 py-3 text-center">
                        <GradeBadge category={grade.category} value={grade.value} size="sm" />
                      </td>
                      <td className="px-4 py-3 text-muted-foreground hidden sm:table-cell">{grade.period}</td>
                      <td className="px-4 py-3 text-muted-foreground hidden lg:table-cell max-w-50 truncate">
                        {grade.comments ?? <span className="opacity-40">—</span>}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <button
                            onClick={() => openEdit(grade)}
                            title="Editar"
                            aria-label={`Editar calificación de ${grade.subjectName}`}
                            className="flex h-7 w-7 items-center justify-center rounded-lg hover:bg-muted text-muted-foreground hover:text-foreground transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                          >
                            <Pencil className="h-3.5 w-3.5" />
                          </button>
                          <button
                            onClick={() => openDelete(grade)}
                            title="Eliminar"
                            aria-label={`Eliminar calificación de ${grade.subjectName}`}
                            className="flex h-7 w-7 items-center justify-center rounded-lg hover:bg-red-500/10 text-muted-foreground hover:text-red-500 transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                          >
                            <Trash2 className="h-3.5 w-3.5" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {totalPages > 1 && (
            <div className="flex items-center justify-between px-4 py-3 border-t border-border bg-muted/20">
              <p className="text-xs text-muted-foreground">
                Página {page} de {totalPages} — {gradesData?.totalCount} calificaciones
              </p>
              <div className="flex items-center gap-1">
                <button
                  disabled={page <= 1}
                  onClick={() => setPage((p) => p - 1)}
                  aria-label="Página anterior"
                  className="flex h-7 w-7 items-center justify-center rounded-lg border border-border hover:bg-muted disabled:opacity-40 disabled:cursor-not-allowed transition-colors cursor-pointer"
                >
                  <ChevronLeft className="h-4 w-4" />
                </button>
                <button
                  disabled={page >= totalPages}
                  onClick={() => setPage((p) => p + 1)}
                  aria-label="Página siguiente"
                  className="flex h-7 w-7 items-center justify-center rounded-lg border border-border hover:bg-muted disabled:opacity-40 disabled:cursor-not-allowed transition-colors cursor-pointer"
                >
                  <ChevronRight className="h-4 w-4" />
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {modal === "create" && (
        <Modal title="Nueva calificación" onClose={() => setModal(null)}>
          <form onSubmit={createForm.handleSubmit((d) => createMutation.mutate(d))} className="space-y-4">
            <Field label="Estudiante" error={createForm.formState.errors.studentId?.message}>
              <div className="relative">
                <select {...createForm.register("studentId")} className={`${inputCls} appearance-none cursor-pointer pr-8`}>
                  <option value="">— Seleccionar —</option>
                  {students.map((s) => (
                    <option key={s.id} value={s.id}>{s.studentCode} · {s.fullName}</option>
                  ))}
                </select>
                <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
              </div>
            </Field>
            <Field label="Sección" error={createForm.formState.errors.sectionId?.message}>
              <div className="relative">
                <select {...createForm.register("sectionId")} className={`${inputCls} appearance-none cursor-pointer pr-8`}>
                  <option value="">— Seleccionar sección —</option>
                  {(sections ?? []).map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.subjectCode} · {s.subjectName} (Sec. {s.sectionCode} — {DAY_LABELS[s.dayOfWeek]})
                    </option>
                  ))}
                </select>
                <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
              </div>
            </Field>
            <div className="grid grid-cols-2 gap-3">
              <Field label="Calificación (0–10)" error={createForm.formState.errors.value?.message}>
                <input
                  {...createForm.register("value", { valueAsNumber: true })}
                  type="number"
                  step="0.1"
                  min={0}
                  max={10}
                  placeholder="8.5"
                  className={inputCls}
                />
              </Field>
              <Field label="Período" error={createForm.formState.errors.period?.message}>
                <input {...createForm.register("period")} placeholder="2025-1" className={inputCls} />
              </Field>
            </div>
            <Field label="Comentarios (opcional)" error={createForm.formState.errors.comments?.message}>
              <textarea
                {...createForm.register("comments")}
                rows={2}
                placeholder="Observaciones sobre el desempeño..."
                className={`${inputCls} h-auto py-2 resize-none`}
              />
            </Field>
            <div className="flex gap-3 pt-1">
              <button type="button" onClick={() => setModal(null)} className="flex-1 h-9 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors cursor-pointer">
                Cancelar
              </button>
              <button
                type="submit"
                disabled={createMutation.isPending}
                className="flex-1 h-9 rounded-lg bg-primary text-primary-foreground text-sm font-semibold hover:bg-primary/90 disabled:opacity-50 transition-colors cursor-pointer flex items-center justify-center gap-2"
              >
                {createMutation.isPending && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
                {createMutation.isPending ? "Guardando..." : "Registrar"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      <EditDeleteModals
        modal={modal === "edit" || modal === "delete" ? modal : null}
        selectedGrade={selectedGrade ? { id: selectedGrade.id, subjectName: selectedGrade.subjectName, value: selectedGrade.value, comments: selectedGrade.comments } : null}
        onClose={() => setModal(null)}
        onEditSubmit={(d) => selectedGrade && updateMutation.mutate({ id: selectedGrade.id, data: d })}
        onDelete={() => selectedGrade && deleteMutation.mutate(selectedGrade.id)}
        updatePending={updateMutation.isPending}
        deletePending={deleteMutation.isPending}
        isAdmin
      />
    </div>
  )
}

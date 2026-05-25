"use client"

import { useState } from "react"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import { Plus, Pencil, Loader2, ChevronDown, BookOpen, Users } from "lucide-react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { toast } from "sonner"
import { sectionsApi, gradesApi, periodsApi } from "@/lib/api"
import { onMutationError } from "@/lib/mutation-error"
import { GradeBadge } from "@/components/grade-badge"
import { ErrorState } from "@/components/error-state"
import { gradeCategory } from "@/lib/grades"
import { Modal, Field, inputCls } from "@/components/modal"
import type { SectionStudentDto } from "@/types"
import { DAY_LABELS } from "@/types"
import { EditDeleteModals } from "./EditDeleteModals"
import { createSchema, type CreateForm, type EditForm } from "./shared"

export function ProfesorGradesView() {
  const qc = useQueryClient()
  const [selectedSectionId, setSelectedSectionId] = useState("")
  const [modal, setModal] = useState<"create" | "edit" | "delete" | null>(null)
  const [pendingStudent, setPendingStudent] = useState<SectionStudentDto | null>(null)
  const [editingGrade, setEditingGrade] = useState<{ id: string; subjectName: string; value: number; comments: string | null } | null>(null)

  const { data: enrollmentStatus } = useQuery({
    queryKey: ["enrollment-status"],
    queryFn: periodsApi.getStatus,
  })

  const { data: sections = [], isLoading: loadingSections, isError: sectionsError, error: sectionsErr, refetch: refetchSections } = useQuery({
    queryKey: ["sections", "my"],
    queryFn: sectionsApi.getMy,
  })

  const selectedSection = sections.find((s) => s.id === selectedSectionId)

  const {
    data: sectionStudents = [],
    isLoading: loadingStudents,
    isError: studentsError,
    error: studentsErr,
    refetch: refetchStudents,
  } = useQuery({
    queryKey: ["sections", selectedSectionId, "students"],
    queryFn: () => sectionsApi.getStudents(selectedSectionId),
    enabled: !!selectedSectionId,
  })

  const createForm = useForm<CreateForm>({
    resolver: zodResolver(createSchema),
    defaultValues: { period: "" },
  })

  const createMutation = useMutation({
    mutationFn: gradesApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["sections", selectedSectionId, "students"] })
      toast.success("Calificación registrada")
      setModal(null)
    },
    onError: onMutationError,
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: EditForm }) => gradesApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["sections", selectedSectionId, "students"] })
      toast.success("Calificación actualizada")
      setModal(null)
    },
    onError: onMutationError,
  })

  function openGrade(student: SectionStudentDto) {
    setPendingStudent(student)
    createForm.reset({
      studentId: student.studentId,
      sectionId: selectedSectionId,
      value: undefined,
      period: enrollmentStatus?.periodCode ?? "",
      comments: "",
    })
    setModal("create")
  }

  function openEdit(student: SectionStudentDto) {
    if (!student.currentGradeId) return
    setEditingGrade({
      id: student.currentGradeId,
      subjectName: selectedSection?.subjectName ?? "",
      value: student.currentGrade ?? 0,
      comments: student.currentGradeComments,
    })
    setModal("edit")
  }

  if (loadingSections) {
    return (
      <div className="flex justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    )
  }

  if (sectionsError) {
    return <ErrorState error={sectionsErr} onRetry={() => void refetchSections()} />
  }

  if (sections.length === 0) {
    return (
      <div className="bg-card border border-border rounded-2xl p-16 text-center">
        <BookOpen className="h-10 w-10 text-muted-foreground mx-auto mb-3" />
        <p className="text-sm font-semibold text-foreground">Sin secciones asignadas</p>
        <p className="text-xs text-muted-foreground mt-1">Contacta al administrador para que te asigne secciones.</p>
      </div>
    )
  }

  return (
    <div className="space-y-5">
      <div className="bg-card border border-border rounded-2xl p-5 shadow-sm">
        <h3 className="text-sm font-semibold text-foreground mb-3">Seleccionar sección</h3>
        <div className="relative max-w-sm">
          <select
            value={selectedSectionId}
            onChange={(e) => setSelectedSectionId(e.target.value)}
            className={`${inputCls} appearance-none cursor-pointer pr-8`}
          >
            <option value="">— Elegir sección —</option>
            {sections.map((s) => (
              <option key={s.id} value={s.id}>
                {s.subjectCode} · {s.subjectName} (Sec. {s.sectionCode} — {DAY_LABELS[s.dayOfWeek]})
              </option>
            ))}
          </select>
          <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
        </div>
      </div>

      {!selectedSectionId ? (
        <div className="bg-card border border-border rounded-2xl p-14 text-center">
          <Users className="h-8 w-8 text-muted-foreground mx-auto mb-3" />
          <p className="text-sm text-muted-foreground">Selecciona una sección para ver los estudiantes</p>
        </div>
      ) : (
        <>
          {studentsError && <ErrorState error={studentsErr} onRetry={() => void refetchStudents()} />}

          {!studentsError && (
            <div className="bg-card border border-border rounded-2xl shadow-sm overflow-hidden">
              {selectedSection && (
                <div className="px-5 py-3.5 border-b border-border bg-muted/20">
                  <p className="text-sm font-semibold text-foreground">{selectedSection.subjectName}</p>
                  <p className="text-xs text-muted-foreground">
                    {selectedSection.subjectCode} · Sec. {selectedSection.sectionCode} · {DAY_LABELS[selectedSection.dayOfWeek]} {selectedSection.startTime}–{selectedSection.endTime}
                  </p>
                </div>
              )}

              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-border bg-muted/40">
                      <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">Estudiante</th>
                      <th className="text-center px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">Calificación</th>
                      <th className="text-right px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">Acción</th>
                    </tr>
                  </thead>
                  <tbody>
                    {loadingStudents ? (
                      Array.from({ length: 4 }).map((_, i) => (
                        <tr key={i} className="border-b border-border/50">
                          {[1, 2, 3].map((j) => (
                            <td key={j} className="px-4 py-3">
                              <div className="h-4 bg-muted animate-pulse rounded" />
                            </td>
                          ))}
                        </tr>
                      ))
                    ) : sectionStudents.length === 0 ? (
                      <tr>
                        <td colSpan={3} className="px-4 py-12 text-center text-sm text-muted-foreground">
                          No hay estudiantes inscritos en esta sección
                        </td>
                      </tr>
                    ) : (
                      sectionStudents.map((student) => (
                        <tr key={student.studentId} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                          <td className="px-4 py-3">
                            <p className="font-medium text-foreground">{student.fullName}</p>
                            <p className="text-xs text-muted-foreground">{student.studentCode}</p>
                          </td>
                          <td className="px-4 py-3 text-center">
                            {student.currentGrade !== null ? (
                              <GradeBadge
                                category={gradeCategory(student.currentGrade)}
                                value={student.currentGrade}
                                size="sm"
                              />
                            ) : (
                              <span className="text-xs text-muted-foreground">Sin nota</span>
                            )}
                          </td>
                          <td className="px-4 py-3">
                            <div className="flex items-center justify-end gap-1">
                              {student.currentGradeId ? (
                                <button
                                  onClick={() => openEdit(student)}
                                  title="Editar calificación"
                                  className="flex items-center gap-1.5 h-7 px-2.5 rounded-lg hover:bg-muted text-muted-foreground hover:text-foreground transition-colors cursor-pointer text-xs font-medium"
                                >
                                  <Pencil className="h-3.5 w-3.5" />
                                  Editar
                                </button>
                              ) : (
                                <button
                                  onClick={() => openGrade(student)}
                                  title="Registrar calificación"
                                  className="flex items-center gap-1.5 h-7 px-2.5 rounded-lg bg-primary/10 text-primary hover:bg-primary/20 transition-colors cursor-pointer text-xs font-medium"
                                >
                                  <Plus className="h-3.5 w-3.5" />
                                  Calificar
                                </button>
                              )}
                            </div>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </>
      )}

      {modal === "create" && pendingStudent && (
        <Modal title={`Calificar: ${pendingStudent.fullName}`} onClose={() => setModal(null)} size="sm">
          <form onSubmit={createForm.handleSubmit((d) => createMutation.mutate(d))} className="space-y-4">
            <div className="rounded-lg bg-muted/40 px-3 py-2 text-xs text-muted-foreground">
              <span className="font-medium text-foreground">{selectedSection?.subjectName}</span>
              {" · "}Sec. {selectedSection?.sectionCode}
            </div>
            <input type="hidden" {...createForm.register("studentId")} />
            <input type="hidden" {...createForm.register("sectionId")} />
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

      {modal === "edit" && editingGrade && (
        <EditDeleteModals
          modal="edit"
          selectedGrade={editingGrade}
          onClose={() => setModal(null)}
          onEditSubmit={(d) => updateMutation.mutate({ id: editingGrade.id, data: d })}
          onDelete={() => {}}
          updatePending={updateMutation.isPending}
          deletePending={false}
        />
      )}
    </div>
  )
}

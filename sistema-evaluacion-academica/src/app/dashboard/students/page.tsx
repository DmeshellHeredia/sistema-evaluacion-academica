"use client"

import { useState } from "react"
import { useRef } from "react"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import { Plus, Pencil, Trash2, Search, ChevronLeft, ChevronRight, Loader2, Users } from "lucide-react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { useAuth } from "@/context/auth-context"
import { studentsApi } from "@/lib/api"
import { onMutationError } from "@/lib/mutation-error"
import { deriveStudentEmail } from "@/lib/email-utils"
import { GradeBadge } from "@/components/grade-badge"
import { ErrorState } from "@/components/error-state"
import { Modal, Field, inputCls } from "@/components/modal"
import { CAREERS, type StudentDto } from "@/types"

const careerEnum = z.enum(CAREERS as unknown as [string, ...string[]], {
  error: "Selecciona una carrera válida",
})

const createSchema = z.object({
  password: z.string().min(8, "Mínimo 8 caracteres"),
  firstName: z.string().min(1, "Requerido"),
  lastName: z.string().min(1, "Requerido"),
  career: careerEnum,
  semester: z
    .number({ error: "Debe ser un número" })
    .int()
    .min(1, "Mínimo 1")
    .max(8, "Máximo 8"),
})

const editSchema = z.object({
  career: careerEnum,
  semester: z
    .number({ error: "Debe ser un número" })
    .int()
    .min(1, "Mínimo 1")
    .max(8, "Máximo 8"),
})

type CreateForm = z.infer<typeof createSchema>
type EditForm = z.infer<typeof editSchema>

export default function StudentsPage() {
  const { user } = useAuth()
  const qc = useQueryClient()
  const canEdit = user?.role === "Admin"

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState("")
  const [debouncedSearch, setDebouncedSearch] = useState("")
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const [modal, setModal] = useState<"create" | "edit" | "delete" | null>(null)
  const [selected, setSelected] = useState<StudentDto | null>(null)

  const pageSize = 10

  function handleSearchChange(value: string) {
    setSearch(value)
    if (debounceRef.current) clearTimeout(debounceRef.current)
    debounceRef.current = setTimeout(() => {
      setPage(1)
      setDebouncedSearch(value)
    }, 400)
  }

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["students", page, pageSize, debouncedSearch],
    queryFn: () => studentsApi.getAll(page, pageSize, debouncedSearch || undefined),
  })

  const students = data?.items ?? []
  const totalPages = data?.totalPages ?? 1

  const createMutation = useMutation({
    mutationFn: studentsApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["students"] })
      toast.success("Estudiante creado exitosamente")
      setModal(null)
    },
    onError: onMutationError,
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: EditForm }) =>
      studentsApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["students"] })
      toast.success("Estudiante actualizado")
      setModal(null)
    },
    onError: onMutationError,
  })

  const deleteMutation = useMutation({
    mutationFn: studentsApi.delete,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["students"] })
      toast.success("Estudiante eliminado")
      setModal(null)
      setSelected(null)
    },
    onError: onMutationError,
  })

  const createForm = useForm<CreateForm>({ resolver: zodResolver(createSchema) })
  const editForm = useForm<EditForm>({ resolver: zodResolver(editSchema) })

  const watchedFirst = createForm.watch("firstName")
  const watchedLast = createForm.watch("lastName")
  const previewEmail = deriveStudentEmail(watchedFirst ?? "", watchedLast ?? "")

  if (user && user.role !== "Admin") {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-center space-y-3">
        <Users className="h-12 w-12 text-muted-foreground" />
        <p className="text-sm font-semibold text-foreground">Acceso restringido</p>
        <p className="text-xs text-muted-foreground">Esta sección es solo para administradores.</p>
      </div>
    )
  }

  function openCreate() {
    createForm.reset()
    setModal("create")
  }

  function openEdit(student: StudentDto) {
    setSelected(student)
    editForm.reset({ career: student.career, semester: student.semester })
    setModal("edit")
  }

  function openDelete(student: StudentDto) {
    setSelected(student)
    setModal("delete")
  }

  const colSpan = canEdit ? 7 : 6

  if (isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center gap-3">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <input
            value={search}
            onChange={(e) => handleSearchChange(e.target.value)}
            placeholder="Buscar por nombre, código o carrera..."
            aria-label="Buscar estudiante"
            className="w-full h-10 pl-9 pr-3 rounded-lg border border-input bg-background text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring/50 focus:border-ring transition-colors"
          />
        </div>
        {canEdit && (
          <button
            onClick={openCreate}
            className="flex items-center gap-2 h-10 px-4 rounded-lg bg-primary text-primary-foreground text-sm font-semibold hover:bg-primary/90 transition-colors cursor-pointer shadow-sm shadow-primary/20 shrink-0"
          >
            <Plus className="h-4 w-4" />
            Nuevo estudiante
          </button>
        )}
      </div>

      <div className="bg-card border border-border rounded-2xl shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border bg-muted/40">
                <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">
                  Código
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">
                  Nombre
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide hidden sm:table-cell">
                  Email
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide hidden md:table-cell">
                  Carrera
                </th>
                <th className="text-center px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide hidden md:table-cell">
                  Sem.
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">
                  Promedio
                </th>
                {canEdit && (
                  <th className="text-right px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">
                    Acciones
                  </th>
                )}
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                Array.from({ length: 6 }).map((_, i) => (
                  <tr key={i} className="border-b border-border/50">
                    {Array.from({ length: colSpan }).map((_, j) => (
                      <td key={j} className="px-4 py-3">
                        <div className="h-4 rounded bg-muted animate-pulse" />
                      </td>
                    ))}
                  </tr>
                ))
              ) : students.length === 0 ? (
                <tr>
                  <td colSpan={colSpan} className="px-4 py-14 text-center text-muted-foreground text-sm">
                    {debouncedSearch ? "No se encontraron resultados para tu búsqueda" : "No hay estudiantes registrados"}
                  </td>
                </tr>
              ) : (
                students.map((student) => (
                  <tr
                    key={student.id}
                    className="border-b border-border/50 hover:bg-muted/30 transition-colors"
                  >
                    <td className="px-4 py-3 font-mono text-xs text-muted-foreground">
                      {student.studentCode}
                    </td>
                    <td className="px-4 py-3 font-medium text-foreground">
                      {student.fullName}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground hidden sm:table-cell">
                      {student.email}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground hidden md:table-cell">
                      {student.career}
                    </td>
                    <td className="px-4 py-3 text-center text-muted-foreground hidden md:table-cell">
                      {student.semester}
                    </td>
                    <td className="px-4 py-3">
                      {student.overallAverage !== null ? (
                        <GradeBadge
                          category={student.averageCategory}
                          value={student.overallAverage}
                          size="sm"
                        />
                      ) : (
                        <span className="text-xs text-muted-foreground">—</span>
                      )}
                    </td>
                    {canEdit && (
                      <td className="px-4 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <button
                            onClick={() => openEdit(student)}
                            title="Editar"
                            aria-label={`Editar ${student.fullName}`}
                            className="flex h-7 w-7 items-center justify-center rounded-lg hover:bg-muted text-muted-foreground hover:text-foreground transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                          >
                            <Pencil className="h-3.5 w-3.5" />
                          </button>
                          <button
                            onClick={() => openDelete(student)}
                            title="Eliminar"
                            aria-label={`Eliminar ${student.fullName}`}
                            className="flex h-7 w-7 items-center justify-center rounded-lg hover:bg-red-500/10 text-muted-foreground hover:text-red-500 transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                          >
                            <Trash2 className="h-3.5 w-3.5" />
                          </button>
                        </div>
                      </td>
                    )}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {totalPages > 1 && (
          <div className="flex items-center justify-between px-4 py-3 border-t border-border bg-muted/20">
            <p className="text-xs text-muted-foreground">
              Página {page} de {totalPages} — {data?.totalCount} estudiantes
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

      {modal === "create" && (
        <Modal title="Nuevo estudiante" onClose={() => setModal(null)}>
          <form
            onSubmit={createForm.handleSubmit((d) => createMutation.mutate(d))}
            className="space-y-4"
          >
            <div className="grid grid-cols-2 gap-3">
              <Field label="Nombre" error={createForm.formState.errors.firstName?.message}>
                <input
                  {...createForm.register("firstName")}
                  placeholder="Juan"
                  className={inputCls}
                />
              </Field>
              <Field label="Apellido" error={createForm.formState.errors.lastName?.message}>
                <input
                  {...createForm.register("lastName")}
                  placeholder="Pérez"
                  className={inputCls}
                />
              </Field>
            </div>

            {/* Correo derivado — solo lectura, no editable */}
            <Field label="Correo electrónico (generado automáticamente)">
              <input
                readOnly
                disabled
                value={previewEmail}
                className={`${inputCls} cursor-not-allowed opacity-60 select-none`}
                tabIndex={-1}
              />
            </Field>

            <Field label="Contraseña temporal" error={createForm.formState.errors.password?.message}>
              <input
                {...createForm.register("password")}
                type="password"
                placeholder="Mínimo 8 caracteres"
                className={inputCls}
              />
            </Field>
            <Field label="Carrera" error={createForm.formState.errors.career?.message}>
              <select {...createForm.register("career")} className={inputCls}>
                <option value="">Seleccionar carrera…</option>
                {CAREERS.map((c) => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </select>
            </Field>
            <Field label="Semestre" error={createForm.formState.errors.semester?.message}>
              <input
                {...createForm.register("semester", { valueAsNumber: true })}
                type="number"
                min={1}
                max={8}
                placeholder="1"
                className={inputCls}
              />
            </Field>
            <div className="flex gap-3 pt-1">
              <button
                type="button"
                onClick={() => setModal(null)}
                className="flex-1 h-9 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors cursor-pointer"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={createMutation.isPending}
                className="flex-1 h-9 rounded-lg bg-primary text-primary-foreground text-sm font-semibold hover:bg-primary/90 disabled:opacity-50 transition-colors cursor-pointer flex items-center justify-center gap-2"
              >
                {createMutation.isPending && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
                {createMutation.isPending ? "Creando..." : "Crear estudiante"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {modal === "edit" && selected && (
        <Modal title={`Editar: ${selected.fullName}`} onClose={() => setModal(null)}>
          <form
            onSubmit={editForm.handleSubmit((d) =>
              updateMutation.mutate({ id: selected.id, data: d })
            )}
            className="space-y-4"
          >
            <Field label="Carrera" error={editForm.formState.errors.career?.message}>
              <select {...editForm.register("career")} className={inputCls}>
                {CAREERS.map((c) => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </select>
            </Field>
            <Field label="Semestre" error={editForm.formState.errors.semester?.message}>
              <input
                {...editForm.register("semester", { valueAsNumber: true })}
                type="number"
                min={1}
                max={8}
                className={inputCls}
              />
            </Field>
            <div className="flex gap-3 pt-1">
              <button
                type="button"
                onClick={() => setModal(null)}
                className="flex-1 h-9 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors cursor-pointer"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={updateMutation.isPending}
                className="flex-1 h-9 rounded-lg bg-primary text-primary-foreground text-sm font-semibold hover:bg-primary/90 disabled:opacity-50 transition-colors cursor-pointer flex items-center justify-center gap-2"
              >
                {updateMutation.isPending && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
                {updateMutation.isPending ? "Guardando..." : "Guardar cambios"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {modal === "delete" && selected && (
        <Modal title="Eliminar estudiante" onClose={() => setModal(null)} size="sm">
          <div className="space-y-5">
            <p className="text-sm text-muted-foreground leading-relaxed">
              ¿Eliminar a{" "}
              <span className="font-semibold text-foreground">{selected.fullName}</span>?
              Esta acción desactivará su cuenta y no puede deshacerse.
            </p>
            <div className="flex gap-3">
              <button
                onClick={() => setModal(null)}
                className="flex-1 h-9 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors cursor-pointer"
              >
                Cancelar
              </button>
              <button
                onClick={() => deleteMutation.mutate(selected.id)}
                disabled={deleteMutation.isPending}
                className="flex-1 h-9 rounded-lg bg-destructive text-white text-sm font-semibold hover:bg-destructive/90 disabled:opacity-50 transition-colors cursor-pointer flex items-center justify-center gap-2"
              >
                {deleteMutation.isPending && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
                {deleteMutation.isPending ? "Eliminando..." : "Eliminar"}
              </button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}

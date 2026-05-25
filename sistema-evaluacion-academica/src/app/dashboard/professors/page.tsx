"use client"

import { useState, useCallback } from "react"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import { Plus, Pencil, Trash2, Search, BookOpen, Loader2, ChevronLeft, ChevronRight } from "lucide-react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { professorsApi } from "@/lib/api"
import { onMutationError } from "@/lib/mutation-error"
import { useAuth } from "@/context/auth-context"
import { ErrorState } from "@/components/error-state"
import { Modal, Field, inputCls } from "@/components/modal"
import type { ProfessorDto } from "@/types"

const PAGE_SIZE = 10

const createSchema = z.object({
  email: z.string().email("Correo inválido"),
  password: z.string().min(8, "Mínimo 8 caracteres"),
  firstName: z.string().min(1, "Requerido"),
  lastName: z.string().min(1, "Requerido"),
})

const editSchema = z.object({
  firstName: z.string().min(1, "Requerido"),
  lastName: z.string().min(1, "Requerido"),
})

type CreateForm = z.infer<typeof createSchema>
type EditForm = z.infer<typeof editSchema>

export default function ProfessorsPage() {
  const { user } = useAuth()
  const qc = useQueryClient()

  const [search, setSearch] = useState("")
  const [page, setPage] = useState(1)
  const [modal, setModal] = useState<"create" | "edit" | "delete" | null>(null)
  const [selected, setSelected] = useState<ProfessorDto | null>(null)

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["professors", page, PAGE_SIZE, search],
    queryFn: () => professorsApi.getAll(page, PAGE_SIZE, search || undefined),
    placeholderData: (prev) => prev,
  })

  const invalidate = useCallback(() => {
    qc.invalidateQueries({ queryKey: ["professors"] })
    qc.invalidateQueries({ queryKey: ["professors", 1, 1] })
  }, [qc])

  const createMutation = useMutation({
    mutationFn: professorsApi.create,
    onSuccess: () => {
      invalidate()
      toast.success("Profesor creado exitosamente")
      setModal(null)
    },
    onError: onMutationError,
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: EditForm }) =>
      professorsApi.update(id, data),
    onSuccess: () => {
      invalidate()
      toast.success("Profesor actualizado")
      setModal(null)
    },
    onError: onMutationError,
  })

  const deleteMutation = useMutation({
    mutationFn: professorsApi.delete,
    onSuccess: () => {
      invalidate()
      toast.success("Profesor eliminado")
      setModal(null)
      setSelected(null)
      if (data && data.items.length === 1 && page > 1) setPage((p) => p - 1)
    },
    onError: onMutationError,
  })

  const createForm = useForm<CreateForm>({ resolver: zodResolver(createSchema) })
  const editForm = useForm<EditForm>({ resolver: zodResolver(editSchema) })

  function openCreate() {
    createForm.reset()
    setModal("create")
  }

  function openEdit(professor: ProfessorDto) {
    setSelected(professor)
    editForm.reset({ firstName: professor.firstName, lastName: professor.lastName })
    setModal("edit")
  }

  function openDelete(professor: ProfessorDto) {
    setSelected(professor)
    setModal("delete")
  }

  function handleSearch(value: string) {
    setSearch(value)
    setPage(1)
  }

  if (user?.role !== "Admin")
    return <div className="p-8 text-center text-muted-foreground">Esta página es solo para administradores.</div>

  if (isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />
  }

  const professors = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1
  const hasPrev = data?.hasPreviousPage ?? false
  const hasNext = data?.hasNextPage ?? false

  return (
    <div className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center gap-3">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <input
            value={search}
            onChange={(e) => handleSearch(e.target.value)}
            placeholder="Buscar por nombre o correo..."
            aria-label="Buscar profesor"
            className="w-full h-10 pl-9 pr-3 rounded-lg border border-input bg-background text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring/50 focus:border-ring transition-colors"
          />
        </div>
        <button
          onClick={openCreate}
          className="flex items-center gap-2 h-10 px-4 rounded-lg bg-primary text-primary-foreground text-sm font-semibold hover:bg-primary/90 transition-colors cursor-pointer shadow-sm shadow-primary/20 shrink-0"
        >
          <Plus className="h-4 w-4" />
          Nuevo profesor
        </button>
      </div>

      <div className="bg-card border border-border rounded-2xl shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border bg-muted/40">
                <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">
                  Nombre
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide hidden sm:table-cell">
                  Email
                </th>
                <th className="text-center px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide hidden md:table-cell">
                  Secciones
                </th>
                <th className="text-right px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">
                  Acciones
                </th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                Array.from({ length: PAGE_SIZE }).map((_, i) => (
                  <tr key={i} className="border-b border-border/50">
                    {Array.from({ length: 4 }).map((_, j) => (
                      <td key={j} className="px-4 py-3">
                        <div className="h-4 rounded bg-muted animate-pulse" />
                      </td>
                    ))}
                  </tr>
                ))
              ) : professors.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-4 py-14 text-center text-muted-foreground text-sm">
                    {search ? "No se encontraron resultados" : "No hay profesores registrados"}
                  </td>
                </tr>
              ) : (
                professors.map((professor) => (
                  <tr
                    key={professor.id}
                    className="border-b border-border/50 hover:bg-muted/30 transition-colors"
                  >
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10 text-primary text-xs font-bold shrink-0">
                          {professor.firstName.charAt(0)}{professor.lastName.charAt(0)}
                        </div>
                        <span className="font-medium text-foreground">{professor.fullName}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-muted-foreground hidden sm:table-cell">
                      {professor.email}
                    </td>
                    <td className="px-4 py-3 text-center hidden md:table-cell">
                      <div className="flex items-center justify-center gap-1 text-muted-foreground">
                        <BookOpen className="h-3.5 w-3.5" />
                        <span className="text-sm">{professor.sectionCount}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-1">
                        <button
                          onClick={() => openEdit(professor)}
                          title="Editar"
                          aria-label={`Editar ${professor.fullName}`}
                          className="flex h-7 w-7 items-center justify-center rounded-lg hover:bg-muted text-muted-foreground hover:text-foreground transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                        >
                          <Pencil className="h-3.5 w-3.5" />
                        </button>
                        <button
                          onClick={() => openDelete(professor)}
                          title="Eliminar"
                          aria-label={`Eliminar ${professor.fullName}`}
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

        {!isLoading && totalCount > 0 && (
          <div className="flex items-center justify-between px-4 py-3 border-t border-border">
            <p className="text-xs text-muted-foreground">
              {totalCount} profesor{totalCount !== 1 ? "es" : ""} · página {page} de {totalPages}
            </p>
            <div className="flex items-center gap-1">
              <button
                onClick={() => setPage((p) => p - 1)}
                disabled={!hasPrev}
                aria-label="Página anterior"
                className="flex h-7 w-7 items-center justify-center rounded-lg border border-border hover:bg-muted disabled:opacity-40 disabled:cursor-not-allowed transition-colors cursor-pointer"
              >
                <ChevronLeft className="h-3.5 w-3.5" />
              </button>
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={!hasNext}
                aria-label="Página siguiente"
                className="flex h-7 w-7 items-center justify-center rounded-lg border border-border hover:bg-muted disabled:opacity-40 disabled:cursor-not-allowed transition-colors cursor-pointer"
              >
                <ChevronRight className="h-3.5 w-3.5" />
              </button>
            </div>
          </div>
        )}
      </div>

      {modal === "create" && (
        <Modal title="Nuevo profesor" onClose={() => setModal(null)}>
          <form
            onSubmit={createForm.handleSubmit((d) => createMutation.mutate(d))}
            className="space-y-4"
          >
            <div className="grid grid-cols-2 gap-3">
              <Field label="Nombre" error={createForm.formState.errors.firstName?.message}>
                <input
                  {...createForm.register("firstName")}
                  placeholder="Ana"
                  className={inputCls}
                />
              </Field>
              <Field label="Apellido" error={createForm.formState.errors.lastName?.message}>
                <input
                  {...createForm.register("lastName")}
                  placeholder="García"
                  className={inputCls}
                />
              </Field>
            </div>
            <Field label="Correo electrónico" error={createForm.formState.errors.email?.message}>
              <input
                {...createForm.register("email")}
                type="email"
                placeholder="ana.garcia@academia.com"
                className={inputCls}
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
                {createMutation.isPending ? "Creando..." : "Crear profesor"}
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
            <div className="grid grid-cols-2 gap-3">
              <Field label="Nombre" error={editForm.formState.errors.firstName?.message}>
                <input
                  {...editForm.register("firstName")}
                  className={inputCls}
                />
              </Field>
              <Field label="Apellido" error={editForm.formState.errors.lastName?.message}>
                <input
                  {...editForm.register("lastName")}
                  className={inputCls}
                />
              </Field>
            </div>
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
        <Modal title="Eliminar profesor" onClose={() => setModal(null)} size="sm">
          <div className="space-y-5">
            <p className="text-sm text-muted-foreground leading-relaxed">
              ¿Eliminar al profesor{" "}
              <span className="font-semibold text-foreground">{selected.fullName}</span>?
              Las secciones asignadas quedarán sin profesor.
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

"use client"

import { useState, useEffect, useCallback } from "react"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import { Plus, Pencil, Trash2, BookOpen, Users, Loader2, Globe, Link2, Search, ChevronLeft, ChevronRight } from "lucide-react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { useAuth } from "@/context/auth-context"
import { subjectsApi, getErrorMessage } from "@/lib/api"
import { onMutationError } from "@/lib/mutation-error"
import { ErrorState } from "@/components/error-state"
import { Modal, Field, inputCls } from "@/components/modal"
import { CAREERS, type SubjectDto, type SubjectLookupDto } from "@/types"

const PAGE_SIZE = 9

const createSchema = z
  .object({
    code: z.string().min(2, "Mínimo 2 caracteres").max(10, "Máximo 10 caracteres"),
    name: z.string().min(3, "Mínimo 3 caracteres"),
    description: z.string().min(5, "Mínimo 5 caracteres"),
    credits: z
      .number({ error: "Debe ser un número" })
      .int()
      .min(1, "Mínimo 1")
      .max(10, "Máximo 10"),
    careers: z.array(z.string()),
    semesterLevel: z
      .number({ error: "Debe ser un número" })
      .int()
      .min(1, "Mínimo 1")
      .max(8, "Máximo 8"),
    appliesToAllCareers: z.boolean(),
  })
  .superRefine((data, ctx) => {
    if (!data.appliesToAllCareers && data.careers.length === 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Selecciona al menos una carrera",
        path: ["careers"],
      })
    }
  })

const editSchema = z
  .object({
    name: z.string().min(3, "Mínimo 3 caracteres"),
    description: z.string().min(5, "Mínimo 5 caracteres"),
    credits: z
      .number({ error: "Debe ser un número" })
      .int()
      .min(1, "Mínimo 1")
      .max(10, "Máximo 10"),
    careers: z.array(z.string()),
    semesterLevel: z
      .number({ error: "Debe ser un número" })
      .int()
      .min(1, "Mínimo 1")
      .max(8, "Máximo 8"),
    appliesToAllCareers: z.boolean(),
  })
  .superRefine((data, ctx) => {
    if (!data.appliesToAllCareers && data.careers.length === 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Selecciona al menos una carrera",
        path: ["careers"],
      })
    }
  })

type CreateForm = z.infer<typeof createSchema>
type EditForm = z.infer<typeof editSchema>

function PrerequisiteSelector({
  allSubjects,
  excludeId,
  selected,
  onChange,
}: {
  allSubjects: SubjectLookupDto[]
  excludeId?: string
  selected: string[]
  onChange: (ids: string[]) => void
}) {
  const available = allSubjects.filter((s) => s.id !== excludeId)

  function toggle(id: string) {
    onChange(selected.includes(id) ? selected.filter((x) => x !== id) : [...selected, id])
  }

  if (available.length === 0) {
    return <p className="text-xs text-muted-foreground py-2">No hay otras materias registradas.</p>
  }

  return (
    <div className="rounded-lg border border-border overflow-hidden max-h-48 overflow-y-auto">
      {available.map((s) => {
        const checked = selected.includes(s.id)
        return (
          <label
            key={s.id}
            className={`flex items-center gap-3 px-3 py-2.5 cursor-pointer select-none transition-colors border-b border-border/50 last:border-0 ${
              checked ? "bg-primary/8" : "hover:bg-muted/40"
            }`}
          >
            <input
              type="checkbox"
              checked={checked}
              onChange={() => toggle(s.id)}
              className="h-4 w-4 rounded border-border accent-primary cursor-pointer shrink-0"
            />
            <div className="flex-1 min-w-0">
              <span className="text-sm font-medium text-foreground">{s.name}</span>
              <span className="text-xs text-muted-foreground ml-2">{s.code} · Sem. {s.semesterLevel}</span>
            </div>
          </label>
        )
      })}
    </div>
  )
}

function Pagination({
  page,
  totalPages,
  totalCount,
  pageSize,
  onPage,
}: {
  page: number
  totalPages: number
  totalCount: number
  pageSize: number
  onPage: (p: number) => void
}) {
  if (totalPages <= 1) return null
  const start = (page - 1) * pageSize + 1
  const end = Math.min(page * pageSize, totalCount)

  return (
    <div className="flex items-center justify-between pt-2">
      <p className="text-xs text-muted-foreground">
        {start}–{end} de {totalCount} materia{totalCount !== 1 ? "s" : ""}
      </p>
      <div className="flex items-center gap-1">
        <button
          onClick={() => onPage(page - 1)}
          disabled={page <= 1}
          aria-label="Página anterior"
          className="flex h-8 w-8 items-center justify-center rounded-lg border border-border hover:bg-muted disabled:opacity-40 disabled:cursor-not-allowed transition-colors cursor-pointer"
        >
          <ChevronLeft className="h-4 w-4" />
        </button>
        <span className="text-xs text-muted-foreground px-2">
          {page} / {totalPages}
        </span>
        <button
          onClick={() => onPage(page + 1)}
          disabled={page >= totalPages}
          aria-label="Página siguiente"
          className="flex h-8 w-8 items-center justify-center rounded-lg border border-border hover:bg-muted disabled:opacity-40 disabled:cursor-not-allowed transition-colors cursor-pointer"
        >
          <ChevronRight className="h-4 w-4" />
        </button>
      </div>
    </div>
  )
}

export default function SubjectsPage() {
  const { user } = useAuth()
  const qc = useQueryClient()
  const canEdit = user?.role === "Admin"

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState("")
  const [debouncedSearch, setDebouncedSearch] = useState("")
  const [modal, setModal] = useState<"create" | "edit" | "delete" | null>(null)
  const [selected, setSelected] = useState<SubjectDto | null>(null)
  const [pendingPrereqIds, setPendingPrereqIds] = useState<string[]>([])

  // Retraso en la búsqueda
  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search)
      setPage(1)
    }, 350)
    return () => clearTimeout(t)
  }, [search])

  const { data: paged, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["subjects", page, PAGE_SIZE, debouncedSearch],
    queryFn: () => subjectsApi.getAll(page, PAGE_SIZE, debouncedSearch || undefined),
  })

  // Consulta ligera de materias para el selector de prerequisitos
  const { data: allSubjectsForSelect = [] } = useQuery({
    queryKey: ["subjects-lookup"],
    queryFn: () => subjectsApi.lookup(),
    staleTime: 60_000,
  })

  const { data: currentPrereqs } = useQuery({
    queryKey: ["subjects", selected?.id, "prerequisites"],
    queryFn: () => subjectsApi.getPrerequisites(selected!.id),
    enabled: !!selected?.id && modal === "edit",
  })

  const setPrereqsMutation = useMutation({
    mutationFn: ({ id, ids }: { id: string; ids: string[] }) =>
      subjectsApi.setPrerequisites(id, ids),
    onError: (err) => toast.error(`Prerequisitos: ${getErrorMessage(err)}`),
  })

  const createMutation = useMutation({
    mutationFn: (d: CreateForm) =>
      subjectsApi.create({ ...d, careers: d.careers }),
    onSuccess: async (created) => {
      if (pendingPrereqIds.length > 0) {
        await setPrereqsMutation.mutateAsync({ id: created.id, ids: pendingPrereqIds })
      }
      qc.invalidateQueries({ queryKey: ["subjects"] })
      qc.invalidateQueries({ queryKey: ["subjects-lookup"] })
      toast.success("Materia creada exitosamente")
      setModal(null)
    },
    onError: onMutationError,
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: EditForm }) =>
      subjectsApi.update(id, { ...data, careers: data.careers }),
    onSuccess: async (_, { id }) => {
      await setPrereqsMutation.mutateAsync({ id, ids: pendingPrereqIds })
      qc.invalidateQueries({ queryKey: ["subjects"] })
      qc.invalidateQueries({ queryKey: ["subjects-lookup"] })
      qc.invalidateQueries({ queryKey: ["subjects", id, "prerequisites"] })
      toast.success("Materia actualizada")
      setModal(null)
    },
    onError: onMutationError,
  })

  const deleteMutation = useMutation({
    mutationFn: subjectsApi.delete,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["subjects"] })
      qc.invalidateQueries({ queryKey: ["subjects-lookup"] })
      toast.success("Materia eliminada")
      setModal(null)
      setSelected(null)
    },
    onError: onMutationError,
  })

  const createForm = useForm<CreateForm>({
    resolver: zodResolver(createSchema),
    defaultValues: { appliesToAllCareers: false, careers: [] },
  })
  const editForm = useForm<EditForm>({
    resolver: zodResolver(editSchema),
    defaultValues: { appliesToAllCareers: false, careers: [] },
  })

  const createAllCareers = createForm.watch("appliesToAllCareers")
  const editAllCareers = editForm.watch("appliesToAllCareers")
  const createSelectedCareers = createForm.watch("careers") ?? []
  const editSelectedCareers = editForm.watch("careers") ?? []

  const openCreate = useCallback(() => {
    createForm.reset({ appliesToAllCareers: false, careers: [] })
    setPendingPrereqIds([])
    setSelected(null)
    setModal("create")
  }, [createForm])

  function openEdit(subject: SubjectDto) {
    setSelected(subject)
    const initialCareers = subject.careers.length > 0 ? subject.careers : []
    editForm.reset({
      name: subject.name,
      description: subject.description,
      credits: subject.credits,
      careers: initialCareers,
      semesterLevel: subject.semesterLevel,
      appliesToAllCareers: subject.appliesToAllCareers,
    })
    setPendingPrereqIds(currentPrereqs?.map((p) => p.subjectId) ?? [])
    setModal("edit")
  }

  useEffect(() => {
    if (modal === "edit" && currentPrereqs) {
      setPendingPrereqIds(currentPrereqs.map((p) => p.subjectId))
    }
  }, [currentPrereqs, modal])

  function openDelete(subject: SubjectDto) {
    setSelected(subject)
    setModal("delete")
  }

  if (isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />
  }

  const items = paged?.items ?? []
  const totalCount = paged?.totalCount ?? 0
  const totalPages = paged?.totalPages ?? 1

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between gap-3">
        <div className="relative flex-1 max-w-xs">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Buscar materia…"
            aria-label="Buscar materia"
            className="w-full h-9 pl-9 pr-3 rounded-lg border border-border bg-card text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>
        {canEdit && (
          <button
            onClick={openCreate}
            className="flex items-center gap-2 h-9 px-4 rounded-lg bg-primary text-primary-foreground text-sm font-semibold hover:bg-primary/90 transition-colors cursor-pointer shadow-sm shadow-primary/20 shrink-0"
          >
            <Plus className="h-4 w-4" />
            Nueva materia
          </button>
        )}
      </div>

      {!isLoading && (
        <p className="text-xs text-muted-foreground -mt-2">
          {totalCount > 0
            ? `${totalCount} materia${totalCount !== 1 ? "s" : ""} registrada${totalCount !== 1 ? "s" : ""}${debouncedSearch ? ` · búsqueda: "${debouncedSearch}"` : ""}`
            : debouncedSearch ? `Sin resultados para "${debouncedSearch}"` : "No hay materias registradas"}
        </p>
      )}

      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: PAGE_SIZE }).map((_, i) => (
            <div key={i} className="bg-card border border-border rounded-2xl p-5 space-y-3">
              <div className="h-5 bg-muted animate-pulse rounded w-3/4" />
              <div className="h-4 bg-muted animate-pulse rounded w-1/2" />
              <div className="h-4 bg-muted animate-pulse rounded" />
            </div>
          ))}
        </div>
      ) : items.length === 0 ? (
        <div className="bg-card border border-border rounded-2xl p-14 text-center">
          <BookOpen className="h-8 w-8 text-muted-foreground mx-auto mb-3" />
          <p className="text-sm text-muted-foreground">
            {debouncedSearch ? `Sin resultados para "${debouncedSearch}"` : "No hay materias registradas"}
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {items.map((subject) => (
            <SubjectCard
              key={subject.id}
              subject={subject}
              canEdit={canEdit}
              onEdit={openEdit}
              onDelete={openDelete}
            />
          ))}
        </div>
      )}

      <Pagination
        page={page}
        totalPages={totalPages}
        totalCount={totalCount}
        pageSize={PAGE_SIZE}
        onPage={setPage}
      />

      {modal === "create" && (
        <Modal title="Nueva materia" onClose={() => setModal(null)}>
          <form
            onSubmit={createForm.handleSubmit((d) => createMutation.mutate(d))}
            className="space-y-4"
          >
            <div className="grid grid-cols-3 gap-3">
              <div className="col-span-2">
                <Field label="Nombre de la materia" error={createForm.formState.errors.name?.message}>
                  <input {...createForm.register("name")} placeholder="Cálculo Diferencial" className={inputCls} />
                </Field>
              </div>
              <Field label="Créditos" error={createForm.formState.errors.credits?.message}>
                <input
                  {...createForm.register("credits", { valueAsNumber: true })}
                  type="number" min={1} max={10} placeholder="4" className={inputCls}
                />
              </Field>
            </div>
            <div className="grid grid-cols-3 gap-3">
              <Field label="Código" error={createForm.formState.errors.code?.message}>
                <input {...createForm.register("code")} placeholder="MAT101" className={inputCls} />
              </Field>
              <Field label="Semestre" error={createForm.formState.errors.semesterLevel?.message}>
                <input
                  {...createForm.register("semesterLevel", { valueAsNumber: true })}
                  type="number" min={1} max={8} placeholder="1" className={inputCls}
                />
              </Field>
              <div />
            </div>

            <label className="flex items-center gap-2.5 cursor-pointer select-none">
              <input
                type="checkbox"
                {...createForm.register("appliesToAllCareers")}
                className="h-4 w-4 rounded border-border accent-primary cursor-pointer"
              />
              <span className="text-sm text-foreground">Aplica a todas las carreras</span>
            </label>

            {!createAllCareers && (
              <Field label="Carreras" error={createForm.formState.errors.careers?.message}>
                <div className="rounded-lg border border-border overflow-hidden">
                  {CAREERS.map((c) => {
                    const checked = createSelectedCareers.includes(c)
                    return (
                      <label
                        key={c}
                        className={`flex items-center gap-3 px-3 py-2.5 cursor-pointer select-none transition-colors border-b border-border/50 last:border-0 ${
                          checked ? "bg-primary/8" : "hover:bg-muted/40"
                        }`}
                      >
                        <input
                          type="checkbox"
                          checked={checked}
                          onChange={(e) => {
                            const current = createForm.getValues("careers") ?? []
                            createForm.setValue(
                              "careers",
                              e.target.checked ? [...current, c] : current.filter((x) => x !== c),
                              { shouldValidate: true }
                            )
                          }}
                          className="h-4 w-4 rounded border-border accent-primary cursor-pointer shrink-0"
                        />
                        <span className="text-sm text-foreground">{c}</span>
                      </label>
                    )
                  })}
                </div>
              </Field>
            )}

            <Field label="Descripción" error={createForm.formState.errors.description?.message}>
              <textarea
                {...createForm.register("description")}
                placeholder="Breve descripción del contenido..."
                rows={2}
                className={`${inputCls} h-auto py-2 resize-none`}
              />
            </Field>

            <div>
              <p className="text-sm font-medium text-foreground mb-1">
                Prerequisitos
                <span className="ml-1.5 text-xs font-normal text-muted-foreground">
                  El estudiante debe aprobar estas materias antes de tomar esta
                </span>
              </p>
              <PrerequisiteSelector
                allSubjects={allSubjectsForSelect}
                selected={pendingPrereqIds}
                onChange={setPendingPrereqIds}
              />
              {pendingPrereqIds.length > 0 && (
                <p className="text-xs text-primary mt-1.5">
                  {pendingPrereqIds.length} prerequisito{pendingPrereqIds.length !== 1 ? "s" : ""} seleccionado{pendingPrereqIds.length !== 1 ? "s" : ""}
                </p>
              )}
            </div>

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
                {createMutation.isPending ? "Creando..." : "Crear materia"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {modal === "edit" && selected && (
        <Modal title={`Editar: ${selected.name}`} onClose={() => setModal(null)}>
          <form
            onSubmit={editForm.handleSubmit((d) =>
              updateMutation.mutate({ id: selected.id, data: d })
            )}
            className="space-y-4"
          >
            <div className="grid grid-cols-3 gap-3">
              <div className="col-span-2">
                <Field label="Nombre de la materia" error={editForm.formState.errors.name?.message}>
                  <input {...editForm.register("name")} className={inputCls} />
                </Field>
              </div>
              <Field label="Créditos" error={editForm.formState.errors.credits?.message}>
                <input
                  {...editForm.register("credits", { valueAsNumber: true })}
                  type="number" min={1} max={10} className={inputCls}
                />
              </Field>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <Field label="Semestre" error={editForm.formState.errors.semesterLevel?.message}>
                <input
                  {...editForm.register("semesterLevel", { valueAsNumber: true })}
                  type="number" min={1} max={8} className={inputCls}
                />
              </Field>
              <div />
            </div>

            <label className="flex items-center gap-2.5 cursor-pointer select-none">
              <input
                type="checkbox"
                {...editForm.register("appliesToAllCareers")}
                className="h-4 w-4 rounded border-border accent-primary cursor-pointer"
              />
              <span className="text-sm text-foreground">Aplica a todas las carreras</span>
            </label>

            {!editAllCareers && (
              <Field label="Carreras" error={editForm.formState.errors.careers?.message}>
                <div className="rounded-lg border border-border overflow-hidden">
                  {CAREERS.map((c) => {
                    const checked = editSelectedCareers.includes(c)
                    return (
                      <label
                        key={c}
                        className={`flex items-center gap-3 px-3 py-2.5 cursor-pointer select-none transition-colors border-b border-border/50 last:border-0 ${
                          checked ? "bg-primary/8" : "hover:bg-muted/40"
                        }`}
                      >
                        <input
                          type="checkbox"
                          checked={checked}
                          onChange={(e) => {
                            const current = editForm.getValues("careers") ?? []
                            editForm.setValue(
                              "careers",
                              e.target.checked ? [...current, c] : current.filter((x) => x !== c),
                              { shouldValidate: true }
                            )
                          }}
                          className="h-4 w-4 rounded border-border accent-primary cursor-pointer shrink-0"
                        />
                        <span className="text-sm text-foreground">{c}</span>
                      </label>
                    )
                  })}
                </div>
              </Field>
            )}

            <Field label="Descripción" error={editForm.formState.errors.description?.message}>
              <textarea
                {...editForm.register("description")}
                rows={2}
                className={`${inputCls} h-auto py-2 resize-none`}
              />
            </Field>

            <div>
              <p className="text-sm font-medium text-foreground mb-1">
                Prerequisitos
                <span className="ml-1.5 text-xs font-normal text-muted-foreground">
                  El estudiante debe aprobar estas materias antes de tomar esta
                </span>
              </p>
              <PrerequisiteSelector
                allSubjects={allSubjectsForSelect}
                excludeId={selected.id}
                selected={pendingPrereqIds}
                onChange={setPendingPrereqIds}
              />
              {pendingPrereqIds.length > 0 && (
                <p className="text-xs text-primary mt-1.5">
                  {pendingPrereqIds.length} prerequisito{pendingPrereqIds.length !== 1 ? "s" : ""} seleccionado{pendingPrereqIds.length !== 1 ? "s" : ""}
                </p>
              )}
            </div>

            <div className="flex gap-3 pt-1">
              <button type="button" onClick={() => setModal(null)} className="flex-1 h-9 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors cursor-pointer">
                Cancelar
              </button>
              <button
                type="submit"
                disabled={updateMutation.isPending || setPrereqsMutation.isPending}
                className="flex-1 h-9 rounded-lg bg-primary text-primary-foreground text-sm font-semibold hover:bg-primary/90 disabled:opacity-50 transition-colors cursor-pointer flex items-center justify-center gap-2"
              >
                {(updateMutation.isPending || setPrereqsMutation.isPending) && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
                {(updateMutation.isPending || setPrereqsMutation.isPending) ? "Guardando..." : "Guardar cambios"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {modal === "delete" && selected && (
        <Modal title="Eliminar materia" onClose={() => setModal(null)} size="sm">
          <div className="space-y-5">
            <p className="text-sm text-muted-foreground leading-relaxed">
              ¿Eliminar la materia{" "}
              <span className="font-semibold text-foreground">{selected.name}</span>?
              Se perderán todas las calificaciones asociadas.
            </p>
            <div className="flex gap-3">
              <button onClick={() => setModal(null)} className="flex-1 h-9 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors cursor-pointer">
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

function SubjectCard({
  subject,
  canEdit,
  onEdit,
  onDelete,
}: {
  subject: SubjectDto
  canEdit: boolean
  onEdit: (s: SubjectDto) => void
  onDelete: (s: SubjectDto) => void
}) {
  const { data: prereqs } = useQuery({
    queryKey: ["subjects", subject.id, "prerequisites"],
    queryFn: () => subjectsApi.getPrerequisites(subject.id),
    staleTime: 60_000,
  })

  return (
    <div className="bg-card border border-border rounded-2xl p-5 shadow-sm hover:shadow-md hover:border-primary/20 transition-all group">
      <div className="flex items-start justify-between gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary/10 shrink-0">
          <BookOpen className="h-5 w-5 text-primary" />
        </div>
        {canEdit && (
          <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
            <button
              onClick={() => onEdit(subject)}
              title="Editar"
              aria-label={`Editar ${subject.name}`}
              className="flex h-7 w-7 items-center justify-center rounded-lg hover:bg-muted text-muted-foreground hover:text-foreground transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              <Pencil className="h-3.5 w-3.5" />
            </button>
            <button
              onClick={() => onDelete(subject)}
              title="Eliminar"
              aria-label={`Eliminar ${subject.name}`}
              className="flex h-7 w-7 items-center justify-center rounded-lg hover:bg-red-500/10 text-muted-foreground hover:text-red-500 transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              <Trash2 className="h-3.5 w-3.5" />
            </button>
          </div>
        )}
      </div>

      <div className="mt-3 space-y-1">
        <div className="flex items-center gap-2">
          <p className="font-semibold text-foreground leading-tight">{subject.name}</p>
          {subject.appliesToAllCareers && (
            <span className="inline-flex items-center gap-1 rounded-full bg-blue-500/10 px-2 py-0.5 text-[10px] font-semibold text-blue-600 dark:text-blue-400 shrink-0">
              <Globe className="h-2.5 w-2.5" />
              General
            </span>
          )}
        </div>
        <p className="text-xs text-muted-foreground">
          {subject.code} · {subject.credits} créd. · Sem. {subject.semesterLevel}
        </p>
        <p className="text-xs text-primary/80 font-medium">
          {subject.appliesToAllCareers
            ? "Todas las carreras"
            : subject.careers.join(", ")}
        </p>
        {subject.description && (
          <p className="text-xs text-muted-foreground line-clamp-2 pt-1">{subject.description}</p>
        )}
      </div>

      {prereqs && prereqs.length > 0 && (
        <div className="mt-3 flex items-center gap-1.5 flex-wrap">
          <Link2 className="h-3 w-3 text-amber-500 shrink-0" />
          {prereqs.map((p) => (
            <span
              key={p.subjectId}
              className="inline-flex items-center rounded-full bg-amber-500/10 px-2 py-0.5 text-[10px] font-semibold text-amber-600 dark:text-amber-400"
            >
              {p.code}
            </span>
          ))}
        </div>
      )}

      <div className="mt-4 pt-3 border-t border-border flex items-center justify-between">
        <p className="text-xs text-muted-foreground/60 italic">Sem. {subject.semesterLevel}</p>
        <div className="flex items-center gap-1 text-xs text-muted-foreground shrink-0">
          <Users className="h-3 w-3" />
          {subject.sectionCount} secc.
        </div>
      </div>
    </div>
  )
}

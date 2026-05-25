"use client"

import { useState, useEffect } from "react"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import { Plus, Pencil, Trash2, Loader2, ChevronDown, CalendarDays, Users2, Search, ChevronLeft, ChevronRight } from "lucide-react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { useAuth } from "@/context/auth-context"
import { sectionsApi, subjectsApi, professorsApi } from "@/lib/api"
import { onMutationError } from "@/lib/mutation-error"
import { ErrorState } from "@/components/error-state"
import { Modal, Field, inputCls } from "@/components/modal"
import type { SectionDto, DayOfWeekType } from "@/types"
import { DAY_LABELS } from "@/types"

const MODALITIES = ["Presencial", "Virtual", "Semipresencial"] as const
const DAYS: DayOfWeekType[] = ["Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sabado"]
const PAGE_SIZE = 15

const DAY_VALUES = ["Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sabado", "Domingo"] as const

const createSchema = z.object({
  subjectId: z.string().min(1, "Selecciona una materia"),
  professorId: z.string().min(1, "Selecciona un profesor"),
  sectionCode: z.string().min(1, "Requerido").max(10, "Máx 10 caracteres"),
  dayOfWeek: z.enum(DAY_VALUES, { error: "Selecciona un día" }),
  startTime: z.string().min(1, "Requerido"),
  endTime: z.string().min(1, "Requerido"),
  modality: z.string().min(1, "Selecciona modalidad"),
  capacity: z.number({ error: "Debe ser un número" }).int().min(1, "Mínimo 1").max(100, "Máximo 100"),
  classroom: z.string().max(50).optional(),
})

const updateSchema = z.object({
  dayOfWeek: z.enum(DAY_VALUES, { error: "Selecciona un día" }),
  startTime: z.string().min(1, "Requerido"),
  endTime: z.string().min(1, "Requerido"),
  modality: z.string().min(1, "Selecciona modalidad"),
  capacity: z.number({ error: "Debe ser un número" }).int().min(1, "Mínimo 1").max(100, "Máximo 100"),
  classroom: z.string().max(50).optional(),
})

type CreateForm = z.infer<typeof createSchema>
type UpdateForm = z.infer<typeof updateSchema>

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
        {start}–{end} de {totalCount} sección{totalCount !== 1 ? "es" : ""}
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

export default function SectionsPage() {
  const { user } = useAuth()
  const qc = useQueryClient()
  const [modal, setModal] = useState<"create" | "edit" | "delete" | null>(null)
  const [selected, setSelected] = useState<SectionDto | null>(null)

  if (user?.role !== "Admin") {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-center space-y-4">
        <CalendarDays className="h-12 w-12 text-muted-foreground" />
        <p className="text-sm text-muted-foreground">Acceso restringido a administradores.</p>
      </div>
    )
  }

  return <SectionsAdminView modal={modal} setModal={setModal} selected={selected} setSelected={setSelected} qc={qc} />
}

function SectionsAdminView({
  modal,
  setModal,
  selected,
  setSelected,
  qc,
}: {
  modal: "create" | "edit" | "delete" | null
  setModal: (m: "create" | "edit" | "delete" | null) => void
  selected: SectionDto | null
  setSelected: (s: SectionDto | null) => void
  qc: ReturnType<typeof useQueryClient>
}) {
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState("")
  const [debouncedSearch, setDebouncedSearch] = useState("")

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search)
      setPage(1)
    }, 350)
    return () => clearTimeout(t)
  }, [search])

  const {
    data: paged,
    isLoading,
    isError,
    error,
    refetch,
  } = useQuery({
    queryKey: ["sections", page, PAGE_SIZE, debouncedSearch],
    queryFn: () => sectionsApi.getAll(page, PAGE_SIZE, debouncedSearch || undefined),
  })

  // Consulta ligera de materias para el selector de nueva sección
  const { data: allSubjects = [] } = useQuery({
    queryKey: ["subjects-lookup"],
    queryFn: () => subjectsApi.lookup(),
    staleTime: 60_000,
  })

  const { data: professors = [] } = useQuery({
    queryKey: ["professors-select"],
    queryFn: () => professorsApi.getAll(1, 100).then((r) => r.items),
  })

  const createForm = useForm<CreateForm>({
    resolver: zodResolver(createSchema),
    defaultValues: { modality: "Presencial", capacity: 30 },
  })

  const updateForm = useForm<UpdateForm>({
    resolver: zodResolver(updateSchema),
  })

  const createMutation = useMutation({
    mutationFn: sectionsApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["sections"] })
      toast.success("Sección creada")
      setModal(null)
    },
    onError: onMutationError,
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateForm }) => sectionsApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["sections"] })
      toast.success("Sección actualizada")
      setModal(null)
    },
    onError: onMutationError,
  })

  const deleteMutation = useMutation({
    mutationFn: sectionsApi.delete,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["sections"] })
      toast.success("Sección eliminada")
      setModal(null)
      setSelected(null)
    },
    onError: onMutationError,
  })

  function openCreate() {
    createForm.reset({ modality: "Presencial", capacity: 30 })
    setModal("create")
  }

  function openEdit(section: SectionDto) {
    setSelected(section)
    updateForm.reset({
      dayOfWeek: section.dayOfWeek,
      startTime: section.startTime,
      endTime: section.endTime,
      modality: section.modality,
      capacity: section.capacity,
      classroom: section.classroom ?? "",
    })
    setModal("edit")
  }

  function openDelete(section: SectionDto) {
    setSelected(section)
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
            placeholder="Buscar sección…"
            aria-label="Buscar sección"
            className="w-full h-9 pl-9 pr-3 rounded-lg border border-border bg-card text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>
        <button
          onClick={openCreate}
          className="flex items-center gap-2 h-9 px-4 rounded-lg bg-primary text-primary-foreground text-sm font-semibold hover:bg-primary/90 transition-colors cursor-pointer shadow-sm shadow-primary/20 shrink-0"
        >
          <Plus className="h-4 w-4" />
          Nueva sección
        </button>
      </div>

      {!isLoading && (
        <p className="text-xs text-muted-foreground -mt-2">
          {totalCount > 0
            ? `${totalCount} sección${totalCount !== 1 ? "es" : ""} registrada${totalCount !== 1 ? "s" : ""}${debouncedSearch ? ` · búsqueda: "${debouncedSearch}"` : ""}`
            : debouncedSearch ? `Sin resultados para "${debouncedSearch}"` : "No hay secciones registradas"}
        </p>
      )}

      {isLoading ? (
        <div className="bg-card border border-border rounded-2xl shadow-sm overflow-hidden animate-pulse">
          <div className="border-b border-border bg-muted/40 px-4 py-3 flex gap-8">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="h-3 w-20 bg-muted rounded" />
            ))}
          </div>
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="border-b border-border/50 px-4 py-4 flex items-center gap-8">
              <div className="space-y-1.5 flex-1">
                <div className="h-3.5 w-48 bg-muted rounded" />
                <div className="h-3 w-32 bg-muted rounded" />
              </div>
              <div className="h-3 w-32 bg-muted rounded hidden md:block" />
              <div className="h-3 w-28 bg-muted rounded hidden sm:block" />
              <div className="h-5 w-20 rounded-full bg-muted hidden lg:block" />
            </div>
          ))}
        </div>
      ) : (
        <div className="bg-card border border-border rounded-2xl shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border bg-muted/40">
                  <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">Materia / Sección</th>
                  <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide hidden md:table-cell">Profesor</th>
                  <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide hidden sm:table-cell">Horario</th>
                  <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide hidden lg:table-cell">Modalidad / Aula</th>
                  <th className="text-center px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">Inscritos</th>
                  <th className="text-right px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {items.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-4 py-16 text-center text-sm text-muted-foreground">
                      {debouncedSearch ? `Sin resultados para "${debouncedSearch}"` : "No hay secciones registradas. Crea la primera."}
                    </td>
                  </tr>
                ) : (
                  items.map((section) => {
                    const occupancy = section.capacity > 0 ? Math.round((section.enrolledCount / section.capacity) * 100) : 0
                    return (
                      <tr key={section.id} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                        <td className="px-4 py-3">
                          <p className="font-medium text-foreground">{section.subjectName}</p>
                          <p className="text-xs text-muted-foreground font-mono">{section.subjectCode} · Sec. {section.sectionCode}</p>
                        </td>
                        <td className="px-4 py-3 text-muted-foreground hidden md:table-cell">{section.professorName}</td>
                        <td className="px-4 py-3 text-muted-foreground hidden sm:table-cell">
                          <p>{DAY_LABELS[section.dayOfWeek]}</p>
                          <p className="text-xs">{section.startTime}–{section.endTime}</p>
                        </td>
                        <td className="px-4 py-3 hidden lg:table-cell">
                          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-primary/10 text-primary">
                            {section.modality}
                          </span>
                          {section.classroom && (
                            <p className="text-xs text-muted-foreground mt-0.5">{section.classroom}</p>
                          )}
                        </td>
                        <td className="px-4 py-3 text-center">
                          <div className="flex flex-col items-center gap-1">
                            <div className="flex items-center gap-1 text-xs text-muted-foreground">
                              <Users2 className="h-3.5 w-3.5" />
                              <span>{section.enrolledCount}/{section.capacity}</span>
                            </div>
                            <div className="w-16 h-1.5 rounded-full bg-muted overflow-hidden">
                              <div
                                className={`h-full rounded-full transition-all ${occupancy >= 90 ? "bg-red-500" : occupancy >= 70 ? "bg-amber-500" : "bg-emerald-500"}`}
                                style={{ width: `${occupancy}%` }}
                              />
                            </div>
                          </div>
                        </td>
                        <td className="px-4 py-3">
                          <div className="flex items-center justify-end gap-1">
                            <button
                              onClick={() => openEdit(section)}
                              aria-label={`Editar sección ${section.subjectCode} · ${section.sectionCode}`}
                              className="flex h-7 w-7 items-center justify-center rounded-lg hover:bg-muted text-muted-foreground hover:text-foreground transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                            >
                              <Pencil className="h-3.5 w-3.5" />
                            </button>
                            <button
                              onClick={() => openDelete(section)}
                              aria-label={`Eliminar sección ${section.subjectCode} · ${section.sectionCode}`}
                              className="flex h-7 w-7 items-center justify-center rounded-lg hover:bg-red-500/10 text-muted-foreground hover:text-red-500 transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                            >
                              <Trash2 className="h-3.5 w-3.5" />
                            </button>
                          </div>
                        </td>
                      </tr>
                    )
                  })
                )}
              </tbody>
            </table>
          </div>
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
        <Modal title="Nueva sección" onClose={() => setModal(null)}>
          <form onSubmit={createForm.handleSubmit((d) => createMutation.mutate(d))} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div className="col-span-2">
                <Field label="Materia" error={createForm.formState.errors.subjectId?.message}>
                  <div className="relative">
                    <select {...createForm.register("subjectId")} className={`${inputCls} appearance-none cursor-pointer pr-8`}>
                      <option value="">— Seleccionar —</option>
                      {allSubjects.map((s) => (
                        <option key={s.id} value={s.id}>{s.code} · {s.name}</option>
                      ))}
                    </select>
                    <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
                  </div>
                </Field>
              </div>
              <div className="col-span-2">
                <Field label="Profesor" error={createForm.formState.errors.professorId?.message}>
                  <div className="relative">
                    <select {...createForm.register("professorId")} className={`${inputCls} appearance-none cursor-pointer pr-8`}>
                      <option value="">— Seleccionar —</option>
                      {professors.map((p) => (
                        <option key={p.id} value={p.id}>{p.fullName}</option>
                      ))}
                    </select>
                    <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
                  </div>
                </Field>
              </div>
              <Field label="Código de sección" error={createForm.formState.errors.sectionCode?.message}>
                <input {...createForm.register("sectionCode")} placeholder="A" className={inputCls} />
              </Field>
              <Field label="Modalidad" error={createForm.formState.errors.modality?.message}>
                <div className="relative">
                  <select {...createForm.register("modality")} className={`${inputCls} appearance-none cursor-pointer pr-8`}>
                    {MODALITIES.map((m) => <option key={m} value={m}>{m}</option>)}
                  </select>
                  <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
                </div>
              </Field>
              <Field label="Día" error={createForm.formState.errors.dayOfWeek?.message}>
                <div className="relative">
                  <select {...createForm.register("dayOfWeek")} className={`${inputCls} appearance-none cursor-pointer pr-8`}>
                    <option value="">— Día —</option>
                    {DAYS.map((d) => <option key={d} value={d}>{DAY_LABELS[d]}</option>)}
                  </select>
                  <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
                </div>
              </Field>
              <Field label="Capacidad" error={createForm.formState.errors.capacity?.message}>
                <input {...createForm.register("capacity", { valueAsNumber: true })} type="number" min={1} max={100} className={inputCls} />
              </Field>
              <Field label="Hora inicio" error={createForm.formState.errors.startTime?.message}>
                <input {...createForm.register("startTime")} type="time" className={inputCls} />
              </Field>
              <Field label="Hora fin" error={createForm.formState.errors.endTime?.message}>
                <input {...createForm.register("endTime")} type="time" className={inputCls} />
              </Field>
              <div className="col-span-2">
                <Field label="Aula (opcional)">
                  <input {...createForm.register("classroom")} placeholder="Ej: A-101" className={inputCls} />
                </Field>
              </div>
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
                {createMutation.isPending ? "Creando..." : "Crear sección"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {modal === "edit" && selected && (
        <Modal title={`Editar: ${selected.subjectName} · Sec. ${selected.sectionCode}`} onClose={() => setModal(null)}>
          <form onSubmit={updateForm.handleSubmit((d) => updateMutation.mutate({ id: selected.id, data: d }))} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <Field label="Día" error={updateForm.formState.errors.dayOfWeek?.message}>
                <div className="relative">
                  <select {...updateForm.register("dayOfWeek")} className={`${inputCls} appearance-none cursor-pointer pr-8`}>
                    {DAYS.map((d) => <option key={d} value={d}>{DAY_LABELS[d]}</option>)}
                  </select>
                  <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
                </div>
              </Field>
              <Field label="Modalidad" error={updateForm.formState.errors.modality?.message}>
                <div className="relative">
                  <select {...updateForm.register("modality")} className={`${inputCls} appearance-none cursor-pointer pr-8`}>
                    {MODALITIES.map((m) => <option key={m} value={m}>{m}</option>)}
                  </select>
                  <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
                </div>
              </Field>
              <Field label="Hora inicio" error={updateForm.formState.errors.startTime?.message}>
                <input {...updateForm.register("startTime")} type="time" className={inputCls} />
              </Field>
              <Field label="Hora fin" error={updateForm.formState.errors.endTime?.message}>
                <input {...updateForm.register("endTime")} type="time" className={inputCls} />
              </Field>
              <Field label="Capacidad" error={updateForm.formState.errors.capacity?.message}>
                <input {...updateForm.register("capacity", { valueAsNumber: true })} type="number" min={1} max={100} className={inputCls} />
              </Field>
              <div className="col-span-2">
                <Field label="Aula (opcional)">
                  <input {...updateForm.register("classroom")} placeholder="Ej: A-101" className={inputCls} />
                </Field>
              </div>
            </div>
            <div className="flex gap-3 pt-1">
              <button type="button" onClick={() => setModal(null)} className="flex-1 h-9 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors cursor-pointer">
                Cancelar
              </button>
              <button
                type="submit"
                disabled={updateMutation.isPending}
                className="flex-1 h-9 rounded-lg bg-primary text-primary-foreground text-sm font-semibold hover:bg-primary/90 disabled:opacity-50 transition-colors cursor-pointer flex items-center justify-center gap-2"
              >
                {updateMutation.isPending && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
                {updateMutation.isPending ? "Guardando..." : "Guardar"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {modal === "delete" && selected && (
        <Modal title="Eliminar sección" onClose={() => setModal(null)} size="sm">
          <div className="space-y-5">
            <p className="text-sm text-muted-foreground leading-relaxed">
              ¿Eliminar la sección{" "}
              <span className="font-semibold text-foreground">{selected.subjectCode} · Sec. {selected.sectionCode}</span>?{" "}
              {selected.enrolledCount > 0 && (
                <span className="text-amber-600 dark:text-amber-400">
                  Tiene {selected.enrolledCount} estudiante{selected.enrolledCount !== 1 ? "s" : ""} inscritos.
                </span>
              )}
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

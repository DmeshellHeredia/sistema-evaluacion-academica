import { z } from "zod"

export const createSchema = z.object({
  studentId: z.string().min(1, "Selecciona un estudiante"),
  sectionId: z.string().min(1, "Selecciona una sección"),
  value: z
    .number({ error: "Debe ser un número" })
    .min(0, "Mínimo 0")
    .max(10, "Máximo 10"),
  period: z.string().min(1, "Requerido"),
  comments: z.string().optional(),
})

export const editSchema = z.object({
  value: z
    .number({ error: "Debe ser un número" })
    .min(0, "Mínimo 0")
    .max(10, "Máximo 10"),
  comments: z.string().optional(),
})

export type CreateForm = z.infer<typeof createSchema>
export type EditForm = z.infer<typeof editSchema>

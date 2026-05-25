import axios, { type AxiosError } from "axios"
import { getToken, removeToken, markSessionExpired } from "./auth"
import type {
  LoginRequest,
  LoginResponse,
  StudentDto,
  CreateStudentDto,
  UpdateStudentDto,
  StudentGradeReportDto,
  ProfessorDto,
  CreateProfessorDto,
  UpdateProfessorDto,
  SubjectDto,
  SubjectLookupDto,
  CreateSubjectDto,
  UpdateSubjectDto,
  PrerequisiteDto,
  SectionDto,
  SectionLookupDto,
  CreateSectionDto,
  UpdateSectionDto,
  SectionStudentDto,
  GradeDto,
  CreateGradeDto,
  UpdateGradeDto,
  PagedResult,
  SubjectCatalogItemDto,
  StudentScheduleDto,
  EnrollRequestDto,
  AcademicPeriodDto,
  CreateAcademicPeriodDto,
  UpdateAcademicPeriodDto,
  EnrollmentStatusDto,
  CourseOverviewDto,
  ParticipantDto,
  AnnouncementDto,
  CreateAnnouncementDto,
  UpdateAnnouncementDto,
  ActivityDto,
  CreateActivityDto,
  UpdateActivityDto,
  SubmissionDto,
  StudentSubmissionDto,
  SubmitDto,
  GradeSubmissionDto,
  StudentGradeSuggestionDto,
  MySuggestionDto,
} from "@/types"

// Axios instance
const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000",
  headers: { "Content-Type": "application/json" },
  timeout: 8_000,
})

// Request interceptor: agrega el JWT a cada petición protegida
api.interceptors.request.use((config) => {
  const token = getToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Evita redireccionamientos múltiples cuando varias peticiones concurrentes reciben 401.
// El indicador se reinicia en cada navegación completa de página (se recarga el módulo).
let redirectingDueToExpiry = false

// Response interceptor: maneja errores globalmente
api.interceptors.response.use(
  (res) => res,
  (error: AxiosError) => {
    if (error.response?.status === 401 && !redirectingDueToExpiry) {
      redirectingDueToExpiry = true
      removeToken()
      if (typeof window !== "undefined") {
        markSessionExpired()
        window.location.href = "/login"
      }
    }
    return Promise.reject(error)
  }
)

export function getErrorMessage(error: unknown): string {
  if (!axios.isAxiosError(error)) return "Error inesperado"
  if (!error.response) {
    if (error.code === "ECONNABORTED") return "El servidor tardó demasiado en responder"
    return "Sin conexión con el servidor"
  }
  const status = error.response.status
  const data = error.response.data as { message?: string; errorMessage?: string } | undefined
  const serverMessage = data?.message ?? data?.errorMessage
  if (status === 401) return "La sesión expiró"
  if (status === 403) return serverMessage ?? "No tienes permiso para esta acción"
  if (status === 404) return serverMessage ?? "El recurso no existe"
  if (status === 422) return serverMessage ?? "Datos inválidos"
  if (status === 429) return "Demasiados intentos. Espera un momento"
  if (status >= 500) return "Error del servidor. Intenta de nuevo"
  return serverMessage ?? error.message ?? "Error desconocido"
}

// Auth endpoints
export const authApi = {
  login: async (data: LoginRequest): Promise<LoginResponse> => {
    const res = await api.post<LoginResponse>("/api/auth/login", data)
    return res.data
  },

  changePassword: async (currentPassword: string, newPassword: string): Promise<void> => {
    await api.put("/api/auth/change-password", { currentPassword, newPassword })
  },
}

// Students endpoints
export const studentsApi = {
  me: async (): Promise<StudentDto> => {
    const res = await api.get<StudentDto>("/api/students/me")
    return res.data
  },

  getAll: async (page = 1, pageSize = 10, search?: string): Promise<PagedResult<StudentDto>> => {
    const res = await api.get<PagedResult<StudentDto>>("/api/students", {
      params: { page, pageSize, ...(search ? { search } : {}) },
    })
    return res.data
  },

  getById: async (id: string): Promise<StudentDto> => {
    const res = await api.get<StudentDto>(`/api/students/${id}`)
    return res.data
  },

  getGradeReport: async (id: string): Promise<StudentGradeReportDto> => {
    const res = await api.get<StudentGradeReportDto>(`/api/students/${id}/grade-report`)
    return res.data
  },

  create: async (data: CreateStudentDto): Promise<StudentDto> => {
    const res = await api.post<StudentDto>("/api/students", data)
    return res.data
  },

  update: async (id: string, data: UpdateStudentDto): Promise<StudentDto> => {
    const res = await api.put<StudentDto>(`/api/students/${id}`, data)
    return res.data
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/api/students/${id}`)
  },
}

// Professors endpoints
export const professorsApi = {
  getAll: async (page = 1, pageSize = 10, search?: string): Promise<PagedResult<ProfessorDto>> => {
    const res = await api.get<PagedResult<ProfessorDto>>("/api/professors", {
      params: { page, pageSize, ...(search ? { search } : {}) },
    })
    return res.data
  },

  getById: async (id: string): Promise<ProfessorDto> => {
    const res = await api.get<ProfessorDto>(`/api/professors/${id}`)
    return res.data
  },

  create: async (data: CreateProfessorDto): Promise<ProfessorDto> => {
    const res = await api.post<ProfessorDto>("/api/professors", data)
    return res.data
  },

  update: async (id: string, data: UpdateProfessorDto): Promise<ProfessorDto> => {
    const res = await api.put<ProfessorDto>(`/api/professors/${id}`, data)
    return res.data
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/api/professors/${id}`)
  },
}

// Subjects endpoints
export const subjectsApi = {
  getAll: async (page = 1, pageSize = 20, search?: string): Promise<PagedResult<SubjectDto>> => {
    const res = await api.get<PagedResult<SubjectDto>>("/api/subjects", {
      params: { page, pageSize, ...(search ? { search } : {}) },
    })
    return res.data
  },

  lookup: async (search?: string, limit = 50): Promise<SubjectLookupDto[]> => {
    const res = await api.get<SubjectLookupDto[]>("/api/subjects/lookup", {
      params: { ...(search ? { search } : {}), limit },
    })
    return res.data
  },

  getById: async (id: string): Promise<SubjectDto> => {
    const res = await api.get<SubjectDto>(`/api/subjects/${id}`)
    return res.data
  },

  create: async (data: CreateSubjectDto): Promise<SubjectDto> => {
    const res = await api.post<SubjectDto>("/api/subjects", data)
    return res.data
  },

  update: async (id: string, data: UpdateSubjectDto): Promise<SubjectDto> => {
    const res = await api.put<SubjectDto>(`/api/subjects/${id}`, data)
    return res.data
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/api/subjects/${id}`)
  },

  getPrerequisites: async (subjectId: string): Promise<PrerequisiteDto[]> => {
    const res = await api.get<PrerequisiteDto[]>(`/api/subjects/${subjectId}/prerequisites`)
    return res.data
  },

  setPrerequisites: async (subjectId: string, prerequisiteIds: string[]): Promise<PrerequisiteDto[]> => {
    const res = await api.put<PrerequisiteDto[]>(`/api/subjects/${subjectId}/prerequisites`, { prerequisiteIds })
    return res.data
  },
}

// Enrollments endpoints
export const enrollmentsApi = {
  getMySchedule: async (): Promise<StudentScheduleDto> => {
    const res = await api.get<StudentScheduleDto>("/api/enrollments/schedule/me")
    return res.data
  },

  getMyCatalog: async (): Promise<SubjectCatalogItemDto[]> => {
    const res = await api.get<SubjectCatalogItemDto[]>("/api/enrollments/catalog/me")
    return res.data
  },

  getCatalog: async (studentId: string): Promise<SubjectCatalogItemDto[]> => {
    const res = await api.get<SubjectCatalogItemDto[]>(`/api/enrollments/catalog/${studentId}`)
    return res.data
  },

  enroll: async (data: EnrollRequestDto): Promise<{ enrollmentId: string; message: string }> => {
    const res = await api.post<{ enrollmentId: string; message: string }>("/api/enrollments", data)
    return res.data
  },

  unenroll: async (studentId: string, sectionId: string): Promise<void> => {
    await api.delete(`/api/enrollments/${studentId}/${sectionId}`)
  },
}

// Sections endpoints
export const sectionsApi = {
  getAll: async (page = 1, pageSize = 20, search?: string): Promise<PagedResult<SectionDto>> => {
    const res = await api.get<PagedResult<SectionDto>>("/api/sections", {
      params: { page, pageSize, ...(search ? { search } : {}) },
    })
    return res.data
  },

  lookup: async (search?: string, limit = 50): Promise<SectionLookupDto[]> => {
    const res = await api.get<SectionLookupDto[]>("/api/sections/lookup", {
      params: { ...(search ? { search } : {}), limit },
    })
    return res.data
  },

  getMy: async (): Promise<SectionDto[]> => {
    const res = await api.get<SectionDto[]>("/api/sections/my")
    return res.data
  },

  getStudents: async (sectionId: string): Promise<SectionStudentDto[]> => {
    const res = await api.get<SectionStudentDto[]>(`/api/sections/${sectionId}/students`)
    return res.data
  },

  create: async (data: CreateSectionDto): Promise<SectionDto> => {
    const res = await api.post<SectionDto>("/api/sections", data)
    return res.data
  },

  update: async (id: string, data: UpdateSectionDto): Promise<SectionDto> => {
    const res = await api.put<SectionDto>(`/api/sections/${id}`, data)
    return res.data
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/api/sections/${id}`)
  },
}

// Grades endpoints
export const gradesApi = {
  getByStudent: async (studentId: string, page = 1, pageSize = 20, period?: string): Promise<PagedResult<GradeDto>> => {
    const params: Record<string, unknown> = { page, pageSize }
    if (period) params.period = period
    const res = await api.get<PagedResult<GradeDto>>(`/api/grades/student/${studentId}`, { params })
    return res.data
  },

  getBySection: async (sectionId: string, page = 1, pageSize = 50): Promise<PagedResult<GradeDto>> => {
    const res = await api.get<PagedResult<GradeDto>>(`/api/grades/section/${sectionId}`, { params: { page, pageSize } })
    return res.data
  },

  getBySubject: async (subjectId: string, period?: string, page = 1, pageSize = 20): Promise<PagedResult<GradeDto>> => {
    const params: Record<string, unknown> = { page, pageSize }
    if (period) params.period = period
    const res = await api.get<PagedResult<GradeDto>>(`/api/grades/subject/${subjectId}`, { params })
    return res.data
  },

  getStudentAverage: async (studentId: string) => {
    const res = await api.get(`/api/grades/student/${studentId}/average`)
    return res.data as { studentId: string; average: number; category: string }
  },

  create: async (data: CreateGradeDto): Promise<GradeDto> => {
    const res = await api.post<GradeDto>("/api/grades", data)
    return res.data
  },

  update: async (id: string, data: UpdateGradeDto): Promise<GradeDto> => {
    const res = await api.put<GradeDto>(`/api/grades/${id}`, data)
    return res.data
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/api/grades/${id}`)
  },
}

// Academic Periods endpoints
export const periodsApi = {
  getStatus: async (): Promise<EnrollmentStatusDto> => {
    const res = await api.get<EnrollmentStatusDto>("/api/academic-periods/status")
    return res.data
  },

  getAll: async (): Promise<AcademicPeriodDto[]> => {
    const res = await api.get<AcademicPeriodDto[]>("/api/academic-periods")
    return res.data
  },

  create: async (data: CreateAcademicPeriodDto): Promise<AcademicPeriodDto> => {
    const res = await api.post<AcademicPeriodDto>("/api/academic-periods", data)
    return res.data
  },

  update: async (id: string, data: UpdateAcademicPeriodDto): Promise<AcademicPeriodDto> => {
    const res = await api.put<AcademicPeriodDto>(`/api/academic-periods/${id}`, data)
    return res.data
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/api/academic-periods/${id}`)
  },

  open: async (id: string): Promise<AcademicPeriodDto> => {
    const res = await api.post<AcademicPeriodDto>(`/api/academic-periods/${id}/open`)
    return res.data
  },

  close: async (id: string): Promise<AcademicPeriodDto> => {
    const res = await api.post<AcademicPeriodDto>(`/api/academic-periods/${id}/close`)
    return res.data
  },
}

// Courses (LMS) endpoints
export const coursesApi = {
  getOverview: async (sectionId: string): Promise<CourseOverviewDto> => {
    const res = await api.get<CourseOverviewDto>(`/api/courses/${sectionId}`)
    return res.data
  },

  getParticipants: async (sectionId: string): Promise<ParticipantDto[]> => {
    const res = await api.get<ParticipantDto[]>(`/api/courses/${sectionId}/participants`)
    return res.data
  },

  getAnnouncements: async (sectionId: string): Promise<AnnouncementDto[]> => {
    const res = await api.get<AnnouncementDto[]>(`/api/courses/${sectionId}/announcements`)
    return res.data
  },

  createAnnouncement: async (sectionId: string, data: CreateAnnouncementDto): Promise<AnnouncementDto> => {
    const res = await api.post<AnnouncementDto>(`/api/courses/${sectionId}/announcements`, data)
    return res.data
  },

  updateAnnouncement: async (id: string, data: UpdateAnnouncementDto): Promise<AnnouncementDto> => {
    const res = await api.put<AnnouncementDto>(`/api/courses/announcements/${id}`, data)
    return res.data
  },

  deleteAnnouncement: async (id: string): Promise<void> => {
    await api.delete(`/api/courses/announcements/${id}`)
  },

  getActivities: async (sectionId: string): Promise<ActivityDto[]> => {
    const res = await api.get<ActivityDto[]>(`/api/courses/${sectionId}/activities`)
    return res.data
  },

  getActivity: async (activityId: string): Promise<ActivityDto> => {
    const res = await api.get<ActivityDto>(`/api/courses/activities/${activityId}`)
    return res.data
  },

  createActivity: async (sectionId: string, data: CreateActivityDto): Promise<ActivityDto> => {
    const res = await api.post<ActivityDto>(`/api/courses/${sectionId}/activities`, data)
    return res.data
  },

  updateActivity: async (activityId: string, data: UpdateActivityDto): Promise<ActivityDto> => {
    const res = await api.put<ActivityDto>(`/api/courses/activities/${activityId}`, data)
    return res.data
  },

  deleteActivity: async (activityId: string): Promise<void> => {
    await api.delete(`/api/courses/activities/${activityId}`)
  },

  getSubmissions: async (activityId: string): Promise<SubmissionDto[]> => {
    const res = await api.get<SubmissionDto[]>(`/api/courses/activities/${activityId}/submissions`)
    return res.data
  },

  gradeSubmission: async (submissionId: string, data: GradeSubmissionDto): Promise<SubmissionDto> => {
    const res = await api.patch<SubmissionDto>(`/api/courses/submissions/${submissionId}/grade`, data)
    return res.data
  },

  getMySubmissions: async (sectionId: string): Promise<StudentSubmissionDto[]> => {
    const res = await api.get<StudentSubmissionDto[]>(`/api/courses/${sectionId}/my-submissions`)
    return res.data
  },

  submit: async (activityId: string, data: SubmitDto): Promise<SubmissionDto> => {
    const res = await api.post<SubmissionDto>(`/api/courses/activities/${activityId}/submit`, data)
    return res.data
  },

  getGradeSuggestions: async (sectionId: string): Promise<StudentGradeSuggestionDto[]> => {
    const res = await api.get<StudentGradeSuggestionDto[]>(`/api/courses/${sectionId}/grade-suggestions`)
    return res.data
  },

  getMyGradeSuggestion: async (sectionId: string): Promise<MySuggestionDto> => {
    const res = await api.get<MySuggestionDto>(`/api/courses/${sectionId}/my-grade-suggestion`)
    return res.data
  },
}

export default api

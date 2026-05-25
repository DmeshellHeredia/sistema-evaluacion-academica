// ─── Auth ──────────────────────────────────────────────────────────────────
export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  email: string
  fullName: string
  role: string
  expiresAt: string
}

export interface AuthUser {
  id: string
  email: string
  fullName: string
  role: "Admin" | "Profesor" | "Estudiante"
  expiresAt: string
}

// ─── Students ──────────────────────────────────────────────────────────────
export interface StudentDto {
  id: string
  studentCode: string
  fullName: string
  email: string
  career: string
  semester: number
  overallAverage: number | null
  averageCategory: string
  createdAt: string
}

export interface CreateStudentDto {
  password: string
  firstName: string
  lastName: string
  career: string
  semester: number
}

export interface UpdateStudentDto {
  career: string
  semester: number
}

export interface StudentGradeReportDto {
  studentId: string
  studentCode: string
  studentFullName: string
  career: string
  semester: number
  overallAverage: number | null
  averageCategory: string
  subjectGrades: SubjectGradeDto[]
}

export interface SubjectGradeDto {
  subjectCode: string
  subjectName: string
  grade: number
  category: string
  period: string
  comments: string | null
}

// ─── Professors ────────────────────────────────────────────────────────────
export interface ProfessorDto {
  id: string
  firstName: string
  lastName: string
  fullName: string
  email: string
  sectionCount: number
  createdAt: string
}

export interface CreateProfessorDto {
  email: string
  password: string
  firstName: string
  lastName: string
}

export interface UpdateProfessorDto {
  firstName: string
  lastName: string
}

// ─── Careers ───────────────────────────────────────────────────────────────
export const CAREERS = [
  "Ingeniería en Sistemas",
  "Ciberseguridad",
  "Desarrollo de Software",
] as const

export type Career = typeof CAREERS[number]

// ─── Subjects ──────────────────────────────────────────────────────────────
export interface SubjectLookupDto {
  id: string
  code: string
  name: string
  semesterLevel: number
}

export interface SubjectDto {
  id: string
  code: string
  name: string
  description: string
  credits: number
  semesterLevel: number
  appliesToAllCareers: boolean
  careers: string[]
  sectionCount: number
}

export interface PrerequisiteDto {
  subjectId: string
  code: string
  name: string
  credits: number
  semesterLevel: number
}

export interface CreateSubjectDto {
  code: string
  name: string
  description: string
  credits: number
  semesterLevel: number
  appliesToAllCareers?: boolean
  careers?: string[]
}

export interface UpdateSubjectDto {
  name: string
  description: string
  credits: number
  semesterLevel: number
  appliesToAllCareers?: boolean
  careers?: string[]
}

// ─── Days of week ──────────────────────────────────────────────────────────
export type DayOfWeekType =
  | "Lunes"
  | "Martes"
  | "Miercoles"
  | "Jueves"
  | "Viernes"
  | "Sabado"
  | "Domingo"

export const DAY_LABELS: Record<DayOfWeekType, string> = {
  Lunes: "Lunes",
  Martes: "Martes",
  Miercoles: "Miércoles",
  Jueves: "Jueves",
  Viernes: "Viernes",
  Sabado: "Sábado",
  Domingo: "Domingo",
}

// ─── Enrollment ────────────────────────────────────────────────────────────
export type EnrollmentStatus =
  | "Disponible"
  | "Inscrita"
  | "Aprobada"
  | "Reprobada"
  | "Bloqueada"
  | "SemestrePrevio"

export interface PrerequisiteInfoDto {
  subjectId: string
  code: string
  name: string
  yourGrade: number | null
}

export interface SectionSummaryDto {
  sectionId: string
  sectionCode: string
  professorName: string
  dayOfWeek: DayOfWeekType
  startTime: string
  endTime: string
  modality: string
  capacity: number
  enrolledCount: number
  isFull: boolean
  classroom: string
}

export interface SubjectCatalogItemDto {
  subjectId: string
  code: string
  name: string
  description: string
  credits: number
  semesterLevel: number
  availableSections: SectionSummaryDto[]
  status: EnrollmentStatus
  grade: number | null
  period: string | null
  missingPrerequisites: PrerequisiteInfoDto[]
  hasScheduleConflict: boolean
  conflictingWith: string | null
}

export interface EnrolledSectionDto {
  sectionId: string
  subjectId: string
  subjectCode: string
  subjectName: string
  sectionCode: string
  professorName: string
  dayOfWeek: DayOfWeekType
  startTime: string
  endTime: string
  modality: string
  classroom: string
  credits: number
}

export interface StudentScheduleDto {
  sections: EnrolledSectionDto[]
  periodName: string
  isEnrollmentOpen: boolean
}

export interface EnrollRequestDto {
  studentId: string
  sectionId: string
}

// ─── Grades ────────────────────────────────────────────────────────────────
export interface GradeDto {
  id: string
  studentCode: string
  studentFullName: string
  subjectCode: string
  subjectName: string
  value: number
  category: string
  period: string
  comments: string | null
  createdAt: string
}

export interface CreateGradeDto {
  studentId: string
  sectionId: string
  value: number
  period: string
  comments?: string
}

// ─── Section Students ───────────────────────────────────────────────────────
export interface SectionStudentDto {
  studentId: string
  studentCode: string
  fullName: string
  email: string
  currentGrade: number | null
  currentGradeId: string | null
  currentGradeComments: string | null
}

// ─── Sections ───────────────────────────────────────────────────────────────
export interface SectionLookupDto {
  id: string
  subjectCode: string
  subjectName: string
  sectionCode: string
  dayOfWeek: DayOfWeekType
  startTime: string
  endTime: string
}

export interface SectionDto {
  id: string
  subjectId: string
  subjectCode: string
  subjectName: string
  sectionCode: string
  professorId: string
  professorName: string
  dayOfWeek: DayOfWeekType
  startTime: string
  endTime: string
  modality: string
  capacity: number
  enrolledCount: number
  isFull: boolean
  classroom: string
}

export interface CreateSectionDto {
  subjectId: string
  professorId: string
  sectionCode: string
  dayOfWeek: DayOfWeekType
  startTime: string
  endTime: string
  modality: string
  capacity: number
  classroom?: string
}

export interface UpdateSectionDto {
  dayOfWeek: DayOfWeekType
  startTime: string
  endTime: string
  modality: string
  capacity: number
  classroom?: string
}

export interface UpdateGradeDto {
  value: number
  comments?: string
}

// ─── Pagination ────────────────────────────────────────────────────────────
export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

// ─── Academic Periods ───────────────────────────────────────────────────────
export interface AcademicPeriodDto {
  id: string
  name: string
  code: string
  startDate: string
  endDate: string
  isEnrollmentOpen: boolean
  createdAt: string
}

export interface CreateAcademicPeriodDto {
  name: string
  code: string
  startDate: string
  endDate: string
}

export interface UpdateAcademicPeriodDto {
  name: string
  code: string
  startDate: string
  endDate: string
}

export interface EnrollmentStatusDto {
  isOpen: boolean
  periodName: string | null
  periodCode: string | null
  startDate: string | null
  endDate: string | null
}

// ─── LMS / Courses ──────────────────────────────────────────────────────────
export interface CourseOverviewDto {
  sectionId: string
  subjectCode: string
  subjectName: string
  sectionCode: string
  professorName: string
  dayOfWeek: DayOfWeekType
  startTime: string
  endTime: string
  modality: string
  enrolledCount: number
  capacity: number
}

export interface ParticipantDto {
  studentId: string
  studentCode: string
  fullName: string
  email: string
  submissionsCount: number
  gradedCount: number
  averageScore: number | null
}

export interface StudentGradeSuggestionDto {
  studentId: string
  studentCode: string
  studentName: string
  suggestedScore: number | null
  officialGrade: number | null
  completedActivities: number
  totalActivities: number
  totalWeight: number
}

export interface MySuggestionDto {
  suggestedScore: number | null
  officialGrade: number | null
  completedActivities: number
  totalActivities: number
  totalWeight: number
}

export interface AnnouncementDto {
  id: string
  sectionId: string
  title: string
  content: string
  authorId: string
  authorName: string
  isPublished: boolean
  createdAt: string
  updatedAt: string | null
}

export interface CreateAnnouncementDto {
  title: string
  content: string
  isPublished?: boolean
}

export interface UpdateAnnouncementDto {
  title: string
  content: string
  isPublished: boolean
}

export type ActivityType = "Tarea" | "Recurso" | "Cuestionario" | "Foro" | "Evaluacion"
export type SubmissionStatus = "Pendiente" | "Entregada" | "Calificada" | "Cerrada"

export interface ActivityDto {
  id: string
  sectionId: string
  title: string
  description: string
  type: ActivityType
  dueDate: string | null
  maxScore: number
  weight: number
  isPublished: boolean
  resourceUrl: string | null
  totalSubmissions: number
  gradedSubmissions: number
  createdAt: string
}

export interface CreateActivityDto {
  title: string
  description: string
  type: ActivityType
  dueDate?: string | null
  maxScore: number
  weight: number
  isPublished?: boolean
  resourceUrl?: string | null
}

export interface UpdateActivityDto {
  title: string
  description: string
  type: ActivityType
  dueDate?: string | null
  maxScore: number
  weight: number
  isPublished: boolean
  resourceUrl?: string | null
}

export interface SubmissionDto {
  id: string
  activityId: string
  activityTitle: string
  studentId: string
  studentName: string
  studentCode: string
  content: string | null
  submittedAt: string | null
  score: number | null
  maxScore: number
  feedback: string | null
  status: SubmissionStatus
  createdAt: string
}

export interface StudentSubmissionDto {
  activityId: string
  activityTitle: string
  activityType: ActivityType
  dueDate: string | null
  maxScore: number
  weight: number
  submissionId: string | null
  content: string | null
  submittedAt: string | null
  score: number | null
  feedback: string | null
  status: SubmissionStatus
}

export interface SubmitDto {
  content?: string | null
}

export interface GradeSubmissionDto {
  score: number
  feedback?: string | null
}

// ─── API Error ─────────────────────────────────────────────────────────────
export interface ApiError {
  statusCode: number
  message: string
  timestamp?: string
}

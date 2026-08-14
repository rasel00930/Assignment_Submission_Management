export type Role = "Admin" | "Teacher" | "Student";

export enum InstitutionType {
  School = "School",
  College = "College",
}

export enum AssignmentStatus {
  Draft = "Draft",
  Published = "Published",
  Closed = "Closed",
}

export enum SubmissionStatus {
  Submitted = "Submitted",
  Resubmitted = "Resubmitted",
  UnderReview = "UnderReview",
  Graded = "Graded",
  Returned = "Returned",
  Late = "Late",
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: Record<string, string[]>;
}

export interface PagedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface UserSummary {
  id: number;
  fullName: string;
  userName: string;
  email: string;
  institutionId: number;
  institutionName: string;
  roles: Role[];
  academicClassId?: number | null;
  academicClassName?: string | null;
}

export interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  user: UserSummary;
}

export interface AuthSession extends TokenResponse {}

export interface UserResponse {
  id: number;
  fullName: string;
  email: string;
  userName: string;
  isActive: boolean;
  roles: Role[];
  academicClassId?: number | null;
  academicClassName?: string | null;
  createdAtUtc: string;
}

export interface InstitutionResponse {
  id: number;
  name: string;
  code: string;
  type: InstitutionType;
  address?: string | null;
  email?: string | null;
  phone?: string | null;
  logoUrl?: string | null;
}

export interface ClassResponse {
  id: number;
  name: string;
  section?: string | null;
  academicYear: string;
  isActive: boolean;
  studentCount: number;
}

export interface SubjectResponse {
  id: number;
  name: string;
  code: string;
  isActive: boolean;
}

export interface TeacherAssignmentResponse {
  id: number;
  teacherId: number;
  teacherName: string;
  academicClassId: number;
  className: string;
  subjectId: number;
  subjectName: string;
  isActive: boolean;
}

export interface SettingResponse {
  id: number;
  key: string;
  value: string;
  description?: string | null;
}

export interface AssignmentResponse {
  id: number;
  title: string;
  description: string;
  deadlineUtc: string;
  maximumMarks: number;
  status: AssignmentStatus;
  allowResubmission: boolean;
  teacherClassSubjectId: number;
  academicClassId: number;
  className: string;
  section?: string | null;
  subjectId: number;
  subjectName: string;
  teacherId: number;
  teacherName: string;
  submissionCount: number;
  createdAtUtc: string;
}

export interface SubmissionResponse {
  id: number;
  assignmentId: number;
  assignmentTitle: string;
  assignmentMaximumMarks: number;
  studentId: number;
  studentName: string;
  studentUserName: string;
  answerText: string;
  submittedAtUtc: string;
  status: SubmissionStatus;
  marks?: number | null;
  feedback?: string | null;
  reviewedAtUtc?: string | null;
  reviewedByTeacherName?: string | null;
}

export interface PagingParams {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
}

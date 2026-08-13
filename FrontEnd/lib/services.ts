import { api } from "@/lib/api";
import type {
  ApiResponse,
  AssignmentResponse,
  AssignmentStatus,
  ClassResponse,
  InstitutionResponse,
  PagedResponse,
  PagingParams,
  SettingResponse,
  SubmissionResponse,
  SubmissionStatus,
  SubjectResponse,
  TeacherAssignmentResponse,
  TokenResponse,
  UserResponse,
  UserSummary,
} from "@/lib/types";

const data = <T>(response: { data: ApiResponse<T> }) => response.data.data;

export const authService = {
  async login(payload: { userName: string; password: string }) {
    return data(await api.post<ApiResponse<TokenResponse>>("/api/auth/login", payload));
  },
  async me() {
    return data(await api.get<ApiResponse<UserSummary>>("/api/auth/me"));
  },
  async logout(refreshToken: string) {
    return data(await api.post<ApiResponse<null>>("/api/auth/logout", { refreshToken }));
  },
  async changePassword(payload: { currentPassword: string; newPassword: string }) {
    return data(await api.post<ApiResponse<null>>("/api/auth/change-password", payload));
  },
};

export const adminService = {
  async getUsers(params: PagingParams & { role?: string; isActive?: boolean; academicClassId?: number }) {
    return data(await api.get<ApiResponse<PagedResponse<UserResponse>>>("/api/admin/users", { params }));
  },
  async getUser(id: number) {
    return data(await api.get<ApiResponse<UserResponse>>(`/api/admin/users/${id}`));
  },
  async createUser(payload: unknown) {
    return data(await api.post<ApiResponse<UserResponse>>("/api/admin/users", payload));
  },
  async updateUser(id: number, payload: unknown) {
    return data(await api.put<ApiResponse<UserResponse>>(`/api/admin/users/${id}`, payload));
  },
  async setUserActive(id: number, value: boolean) {
    return data(await api.patch<ApiResponse<null>>(`/api/admin/users/${id}/active`, null, { params: { value } }));
  },
  async resetPassword(id: number, newPassword: string) {
    return data(await api.post<ApiResponse<null>>(`/api/admin/users/${id}/reset-password`, { newPassword }));
  },
  async getClasses(includeInactive = false) {
    return data(await api.get<ApiResponse<ClassResponse[]>>("/api/admin/classes", { params: { includeInactive } }));
  },
  async createClass(payload: unknown) {
    return data(await api.post<ApiResponse<ClassResponse>>("/api/admin/classes", payload));
  },
  async updateClass(id: number, payload: unknown) {
    return data(await api.put<ApiResponse<ClassResponse>>(`/api/admin/classes/${id}`, payload));
  },
  async deactivateClass(id: number) {
    return data(await api.delete<ApiResponse<null>>(`/api/admin/classes/${id}`));
  },
  async getSubjects(includeInactive = false) {
    return data(await api.get<ApiResponse<SubjectResponse[]>>("/api/admin/subjects", { params: { includeInactive } }));
  },
  async createSubject(payload: unknown) {
    return data(await api.post<ApiResponse<SubjectResponse>>("/api/admin/subjects", payload));
  },
  async updateSubject(id: number, payload: unknown) {
    return data(await api.put<ApiResponse<SubjectResponse>>(`/api/admin/subjects/${id}`, payload));
  },
  async deactivateSubject(id: number) {
    return data(await api.delete<ApiResponse<null>>(`/api/admin/subjects/${id}`));
  },
  async getTeacherAssignments(params: PagingParams & { teacherId?: number; academicClassId?: number; subjectId?: number; isActive?: boolean }) {
    return data(await api.get<ApiResponse<PagedResponse<TeacherAssignmentResponse>>>("/api/teacher-assignments", { params }));
  },
  async createTeacherAssignment(payload: unknown) {
    return data(await api.post<ApiResponse<TeacherAssignmentResponse>>("/api/teacher-assignments", payload));
  },
  async updateTeacherAssignment(id: number, payload: unknown) {
    return data(await api.put<ApiResponse<TeacherAssignmentResponse>>(`/api/teacher-assignments/${id}`, payload));
  },
  async deactivateTeacherAssignment(id: number) {
    return data(await api.delete<ApiResponse<null>>(`/api/teacher-assignments/${id}`));
  },
  async getInstitution() {
    return data(await api.get<ApiResponse<InstitutionResponse>>("/api/admin/institution"));
  },
  async updateInstitution(payload: unknown) {
    return data(await api.put<ApiResponse<InstitutionResponse>>("/api/admin/institution", payload));
  },
  async getSettings() {
    return data(await api.get<ApiResponse<SettingResponse[]>>("/api/admin/settings"));
  },
  async upsertSetting(payload: unknown) {
    return data(await api.put<ApiResponse<SettingResponse>>("/api/admin/settings", payload));
  },
};

export const assignmentService = {
  async get(params: PagingParams & { status?: AssignmentStatus; academicClassId?: number; subjectId?: number; teacherId?: number }) {
    return data(await api.get<ApiResponse<PagedResponse<AssignmentResponse>>>("/api/assignments", { params }));
  },
  async getById(id: number) {
    return data(await api.get<ApiResponse<AssignmentResponse>>(`/api/assignments/${id}`));
  },
  async create(payload: unknown) {
    return data(await api.post<ApiResponse<AssignmentResponse>>("/api/assignments", payload));
  },
  async update(id: number, payload: unknown) {
    return data(await api.put<ApiResponse<AssignmentResponse>>(`/api/assignments/${id}`, payload));
  },
  async publish(id: number) {
    return data(await api.post<ApiResponse<null>>(`/api/assignments/${id}/publish`));
  },
  async moveToDraft(id: number) {
    return data(await api.post<ApiResponse<null>>(`/api/assignments/${id}/draft`));
  },
  async close(id: number) {
    return data(await api.post<ApiResponse<null>>(`/api/assignments/${id}/close`));
  },
  async remove(id: number) {
    return data(await api.delete<ApiResponse<null>>(`/api/assignments/${id}`));
  },
};

export const submissionService = {
  async get(params: PagingParams & { assignmentId?: number; studentId?: number; status?: SubmissionStatus }) {
    return data(await api.get<ApiResponse<PagedResponse<SubmissionResponse>>>("/api/submissions", { params }));
  },
  async getById(id: number) {
    return data(await api.get<ApiResponse<SubmissionResponse>>(`/api/submissions/${id}`));
  },
  async submit(assignmentId: number, answerText: string) {
    return data(await api.post<ApiResponse<SubmissionResponse>>(`/api/submissions/assignment/${assignmentId}`, { answerText }));
  },
  async review(id: number, payload: { marks?: number | null; feedback?: string | null; status: SubmissionStatus }) {
    return data(await api.put<ApiResponse<SubmissionResponse>>(`/api/submissions/${id}/review`, payload));
  },
};

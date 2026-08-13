"use client";
import { useParams } from "next/navigation";
import { RolePage } from "@/components/auth/role-page";
import { AssignmentDetail } from "@/components/assignments/assignment-detail";
export default function Page(){const p=useParams<{id:string}>();return <RolePage role="Teacher"><AssignmentDetail id={Number(p.id)} mode="teacher"/></RolePage>}

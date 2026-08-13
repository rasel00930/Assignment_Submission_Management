"use client";
import { useParams } from "next/navigation";
import { RolePage } from "@/components/auth/role-page";
import { AssignmentForm } from "@/components/assignments/assignment-form";
export default function Page(){const params=useParams<{id:string}>();return <RolePage role="Teacher"><AssignmentForm id={Number(params.id)}/></RolePage>}

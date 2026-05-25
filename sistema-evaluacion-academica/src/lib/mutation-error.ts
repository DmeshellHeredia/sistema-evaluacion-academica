import { toast } from "sonner"
import { getErrorMessage } from "./api"

export const onMutationError = (err: unknown) => toast.error(getErrorMessage(err))

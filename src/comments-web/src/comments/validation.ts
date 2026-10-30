import type { Messages } from '../messages';
import type { Field, FieldErrors, ValidationCode } from './model';
const fieldCodes: Record<Field, ValidationCode> = {
  userName: 'nameError', email: 'emailError', homePage: 'homeError', text: 'textError',
};
// Provider text is not UI copy. Known fields always receive the selected translation.
export function fieldError(field: Field, code: ValidationCode | undefined, server: FieldErrors, labels: Messages): string | undefined {
  return code ? labels[code] : server[field]?.length ? labels[fieldCodes[field]] : undefined;
}

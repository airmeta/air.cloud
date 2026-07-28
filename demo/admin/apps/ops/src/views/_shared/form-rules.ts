import type { FormInstanceFunctions, FormRule } from 'tdesign-vue-next';

function isBlank(value: unknown) {
  return typeof value !== 'string' ? value === undefined || value === null : value.trim().length === 0;
}

export function requiredRule(message: string, trigger: FormRule['trigger'] = 'blur'): FormRule[] {
  return [
    {
      message,
      required: true,
      trigger,
      whitespace: true,
    },
  ];
}

export function requiredArrayRule(message: string): FormRule[] {
  return [
    {
      message,
      required: true,
      trigger: 'change',
      validator: (value) => Array.isArray(value) && value.length > 0,
    },
  ];
}

export function optionalHttpUrlRule(message: string): FormRule[] {
  return [
    {
      message,
      trigger: 'blur',
      validator: (value) => isBlank(value) || /^https?:\/\//i.test(String(value).trim()),
    },
  ];
}

export function requiredHttpUrlRule(message: string): FormRule[] {
  return [
    {
      message,
      required: true,
      trigger: 'blur',
      validator: (value) => !isBlank(value) && /^https?:\/\//i.test(String(value).trim()),
    },
  ];
}

export function optionalNumberRule(message: string): FormRule[] {
  return [
    {
      message,
      trigger: 'blur',
      validator: (value) => isBlank(value) || Number.isFinite(Number(value)),
    },
  ];
}

export async function validateForm(form: FormInstanceFunctions | undefined) {
  if (!form) {
    return true;
  }

  return (await form.validate()) === true;
}

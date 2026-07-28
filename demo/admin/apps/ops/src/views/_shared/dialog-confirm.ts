import type { DialogOptions } from 'tdesign-vue-next';
import { DialogPlugin } from 'tdesign-vue-next';

type ConfirmHandler = (context: {
  e: KeyboardEvent | MouseEvent;
}) => Promise<void> | void;

type ConfirmAndCloseOptions = Omit<DialogOptions, 'onConfirm'> & {
  onConfirm?: ConfirmHandler;
};

export function confirmAndClose(options: ConfirmAndCloseOptions) {
  let dialog: ReturnType<typeof DialogPlugin.confirm>;
  dialog = DialogPlugin.confirm({
    ...options,
    onConfirm: async (context) => {
      dialog.setConfirmLoading(true);
      try {
        await options.onConfirm?.(context);
        dialog.hide();
      } finally {
        dialog.setConfirmLoading(false);
      }
    },
  });

  return dialog;
}

# Login View Validation Checklist

Use this checklist before opening a pull request that touches the login experience.

- **Mobile viewport:** Test at ~375px width; ensure form fits without horizontal scroll and primary action stays visible above the fold.
- **Successful login:** Submit valid Supabase test credentials; confirm redirect to `/` and cleared busy state.
- **Rejected credentials:** Use a wrong password; verify user-friendly error from `AuthErrorMapper` plus no navigation.
- **Unconfirmed email:** Attempt with an account pending confirmation; ensure message prompts user to confirm email.
- **Network failure:** Simulate offline (DevTools) and retry; expect graceful error and logger entry without crashing the app.
- **Accessibility:** Confirm focus order is logical, keyboard-only submission works, and busy state announcements appear via screen reader (`aria-live`).
- **Cancellation:** Start typing, then navigate away; make sure no pending requests or console errors remain.

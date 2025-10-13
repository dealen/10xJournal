# Reset Password Feature - Supabase Configuration

## Overview
The reset password feature requires configuration in the Supabase dashboard to specify where users should be redirected after clicking the reset link in their email.

## Required Configuration

### 1. Supabase Dashboard Settings

Navigate to your Supabase project dashboard:

**Authentication → URL Configuration → Redirect URLs**

Add the following URLs to the allowed redirect URLs list:

#### Development
```
http://localhost:5000/update-password
```

#### Production
```
https://your-production-domain.com/update-password
```

### 2. Email Template (Optional Customization)

You can customize the password reset email template in:

**Authentication → Email Templates → Reset Password**

The default template includes a link that will redirect to your configured URL.

## How It Works

1. User requests password reset at `/reset-password`
2. System calls `SupabaseClient.Auth.ResetPasswordForEmail(email)`
3. Supabase sends email with magic link
4. User clicks link in email
5. Supabase validates token and redirects to `/update-password`
6. User is automatically signed in with a temporary session
7. User enters new password at `/update-password`
8. System calls `SupabaseClient.Auth.Update()` to change password
9. User is redirected to login page

## Security Notes

- The reset link expires after a set time (configured in Supabase)
- Only the email owner can access the reset link
- Success message doesn't reveal if email exists in system
- Password must meet minimum requirements (6+ characters)
- Old sessions are invalidated after password change

## Testing the Feature

### Local Development Testing

1. Start Supabase locally: `supabase start`
2. Run the application
3. Navigate to `/reset-password`
4. Enter a registered email address
5. Check Inbucket (http://localhost:54324) for the reset email
6. Click the reset link in the email
7. Enter new password at `/update-password`
8. Verify redirect to login page
9. Log in with new password

### Production Testing

Follow the same steps but use your production domain and check actual email delivery.

## Troubleshooting

### Issue: "Invalid or expired link" message
- **Cause:** Reset link has expired or already been used
- **Solution:** Request a new reset link

### Issue: Redirect URL not working
- **Cause:** URL not added to Supabase allowed redirect URLs
- **Solution:** Add the URL in Supabase dashboard settings

### Issue: Email not received
- **Causes:** 
  - Email in spam folder
  - Invalid email address
  - Email service configuration issue
- **Solution:** Check spam, verify email, check Supabase logs

### Issue: Password not updating
- **Cause:** Session invalid or password doesn't meet requirements
- **Solution:** Ensure user came from valid reset link, check password strength

## Related Files

- `/Features/Authentication/ResetPassword/ResetPasswordPage.razor`
- `/Features/Authentication/ResetPassword/ResetPasswordForm.razor`
- `/Features/Authentication/ResetPassword/ResetPasswordModel.cs`
- `/Features/Authentication/UpdatePassword/UpdatePasswordPage.razor`
- `/Features/Authentication/UpdatePassword/UpdatePasswordForm.razor`
- `/Features/Authentication/UpdatePassword/UpdatePasswordModel.cs`

-- ============================================================================
-- migration: create delete_my_account rpc function
-- description: creates a postgresql function to delete user account and all associated data
-- functions created: public.delete_my_account()
-- dependencies: public.journal_entries, public.profiles, auth.users, auth.uid()
-- notes: 
--   - this function can only be called by authenticated users
--   - performs complete account deletion including all user data
--   - respects data integrity with explicit cascade deletion
--   - uses security definer to allow deletion of auth.users
--   - returns json response with success status and message
-- ============================================================================

-- create the delete_my_account function
-- this function deletes the authenticated user's account and all associated data
create or replace function public.delete_my_account()
returns json
language plpgsql
security definer
set search_path = public
as $$
declare
  current_user_id uuid;
  entries_deleted int;
  result json;
begin
  -- ensure the user is authenticated
  current_user_id := auth.uid();
  
  if current_user_id is null then
    raise exception 'not authenticated';
  end if;

  -- log the account deletion attempt
  raise notice 'account deletion initiated for user: %', current_user_id;

  -- delete all journal entries for this user
  -- the on delete cascade on journal_entries should handle this, but we're being explicit
  delete from public.journal_entries 
  where user_id = current_user_id;
  
  get diagnostics entries_deleted = row_count;
  raise notice 'deleted % journal entries for user: %', entries_deleted, current_user_id;

  -- delete the user's profile
  -- this should cascade due to foreign key constraints
  delete from public.profiles 
  where id = current_user_id;

  -- delete the user from auth.users
  -- this will cascade delete auth sessions and related auth data
  -- security definer is required to allow this operation
  delete from auth.users 
  where id = current_user_id;

  -- build success response
  result := json_build_object(
    'success', true,
    'message', 'account successfully deleted',
    'entries_deleted', entries_deleted,
    'deleted_at', now()
  );

  raise notice 'account deletion completed for user: %', current_user_id;

  return result;

exception
  when others then
    -- log the error
    raise warning 'error during account deletion for user %: %', current_user_id, sqlerrm;
    
    -- return error response
    result := json_build_object(
      'success', false,
      'message', 'account deletion failed',
      'error', sqlerrm
    );
    
    return result;
end;
$$;

-- ============================================================================
-- security and permissions
-- ============================================================================

-- grant execute permission to authenticated users only
grant execute on function public.delete_my_account() to authenticated;

-- explicitly revoke from anonymous users for security
revoke execute on function public.delete_my_account() from anon;

-- add comment for documentation
comment on function public.delete_my_account() is 
  'deletes the authenticated user''s account and all associated data. returns json with success status.';

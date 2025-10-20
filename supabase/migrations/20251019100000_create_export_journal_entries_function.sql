-- ============================================================================
-- migration: create export_journal_entries rpc function
-- description: creates a postgresql function to export all journal entries for the authenticated user
-- functions created: public.export_journal_entries()
-- dependencies: public.journal_entries, auth.uid()
-- notes: 
--   - this function can only be called by authenticated users
--   - returns all journal entries for the calling user in json format
--   - respects row level security policies
--   - returns data structure matching the exportdataresponse c# model
-- ============================================================================

-- create the export_journal_entries function
-- this function returns all journal entries for the authenticated user in a structured json format
create or replace function public.export_journal_entries()
returns json
language plpgsql
security definer
set search_path = public
as $$
declare
  result json;
begin
  -- ensure the user is authenticated
  if auth.uid() is null then
    raise exception 'not authenticated';
  end if;

  -- build the json response with total count, export timestamp, and all entries
  select json_build_object(
    'total_entries', count(*),
    'exported_at', now(),
    'entries', coalesce(
      json_agg(
        json_build_object(
          'id', je.id,
          'created_at', je.created_at,
          'content', je.content
        )
        order by je.created_at desc
      ),
      '[]'::json
    )
  )
  into result
  from public.journal_entries je
  where je.user_id = auth.uid();

  return result;
end;
$$;

-- ============================================================================
-- security and permissions
-- ============================================================================

-- grant execute permission to authenticated users only
-- this ensures only logged-in users can call this function
grant execute on function public.export_journal_entries() to authenticated;

-- revoke execute permission from anonymous users
-- this ensures unauthenticated users cannot export data
revoke execute on function public.export_journal_entries() from anon;

-- ============================================================================
-- comments for documentation
-- ============================================================================

comment on function public.export_journal_entries() is 'exports all journal entries for the authenticated user in json format with total_entries count, exported_at timestamp, and entries array. can only be called by authenticated users.';

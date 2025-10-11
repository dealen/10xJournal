-- ============================================================================
-- migration: streak tracking automation
-- description: creates trigger to automatically update user streak when entries are created
-- tables affected: public.journal_entries (trigger source), public.user_streaks
-- dependencies: public.journal_entries, public.user_streaks
-- notes: 
--   - trigger fires when a new journal entry is created
--   - automatically calculates and updates current_streak and longest_streak
--   - handles streak continuation (same day or consecutive day) and streak breaks
--   - uses security definer to bypass rls for streak updates
--   - streak logic: consecutive days with at least one entry counts as a streak
-- ============================================================================

-- create function to handle streak updates when a new entry is created
-- this function implements the core streak calculation logic
create or replace function public.handle_streak_update()
returns trigger
language plpgsql
-- security definer allows this function to bypass rls policies
-- this is necessary to update the user_streaks table on behalf of the user
security definer
set search_path = public
as $$
declare
  v_last_entry_date date;
  v_current_streak integer;
  v_longest_streak integer;
  v_entry_date date;
begin
  -- extract the date (without time) from the new entry's created_at timestamp
  v_entry_date := date(new.created_at);
  
  -- retrieve the current streak data for this user
  select last_entry_date, current_streak, longest_streak
  into v_last_entry_date, v_current_streak, v_longest_streak
  from public.user_streaks
  where user_id = new.user_id;
  
  -- case 1: this is the user's first entry ever (last_entry_date is null)
  if v_last_entry_date is null then
    -- start the streak at 1
    update public.user_streaks
    set 
      current_streak = 1,
      longest_streak = 1,
      last_entry_date = v_entry_date
    where user_id = new.user_id;
    
  -- case 2: the entry is on the same day as the last entry
  -- this does not extend the streak, just update the last_entry_date
  elsif v_entry_date = v_last_entry_date then
    -- no change to streak counts, only update the last_entry_date timestamp
    update public.user_streaks
    set last_entry_date = v_entry_date
    where user_id = new.user_id;
    
  -- case 3: the entry is exactly one day after the last entry (consecutive day)
  -- this continues the streak
  elsif v_entry_date = v_last_entry_date + interval '1 day' then
    -- increment the current streak
    v_current_streak := v_current_streak + 1;
    
    -- update longest_streak if the current streak is now the longest
    if v_current_streak > v_longest_streak then
      v_longest_streak := v_current_streak;
    end if;
    
    -- update the streak record
    update public.user_streaks
    set 
      current_streak = v_current_streak,
      longest_streak = v_longest_streak,
      last_entry_date = v_entry_date
    where user_id = new.user_id;
    
  -- case 4: the entry is more than one day after the last entry (streak broken)
  -- reset the current streak to 1
  else
    -- the streak has been broken, start a new streak
    update public.user_streaks
    set 
      current_streak = 1,
      longest_streak = v_longest_streak, -- keep the historical longest streak
      last_entry_date = v_entry_date
    where user_id = new.user_id;
  end if;
  
  -- return new to allow the insert into journal_entries to proceed
  return new;
end;
$$;

-- create trigger on public.journal_entries to automatically update streaks
-- this trigger fires after a new journal entry is inserted
-- important: this runs after the entry is created to ensure it has a valid created_at timestamp
create trigger on_journal_entry_created
  after insert on public.journal_entries
  for each row
  execute function public.handle_streak_update();

-- ============================================================================
-- comments for documentation
-- ============================================================================

comment on function public.handle_streak_update is 'automatically updates user streak data when a new journal entry is created';

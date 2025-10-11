-- ============================================================================
-- migration: fix streak update trigger issues
-- description: addresses github copilot review feedback for streak calculation
-- tables affected: public.user_streaks (via function update)
-- dependencies: public.handle_streak_update (replaces existing function)
-- notes: 
--   - fixes date arithmetic to use + 1 instead of + interval '1 day' for reliability
--   - adds error handling for missing user_streaks record (defensive programming)
--   - maintains backward compatibility with existing data
-- github copilot feedback addressed:
--   1. date arithmetic: changed from interval to integer addition
--   2. error handling: added check for missing user_streaks record
-- ============================================================================

-- recreate the streak update function with fixes
-- note: using "create or replace" to update the existing function
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
  v_streak_exists boolean;
begin
  -- extract the date (without time) from the new entry's created_at timestamp
  v_entry_date := date(new.created_at);
  
  -- check if a user_streaks record exists for this user
  -- this handles edge cases where the onboarding trigger might have failed
  select exists(
    select 1 
    from public.user_streaks 
    where user_id = new.user_id
  ) into v_streak_exists;
  
  -- if no streak record exists, create one
  -- this is defensive programming - the onboarding trigger should have created it
  if not v_streak_exists then
    insert into public.user_streaks (user_id, current_streak, longest_streak, last_entry_date)
    values (new.user_id, 0, 0, null);
  end if;
  
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
  -- fix: using + 1 instead of + interval '1 day' for reliable date arithmetic with date type
  elsif v_entry_date = v_last_entry_date + 1 then
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

-- ============================================================================
-- comments for documentation
-- ============================================================================

comment on function public.handle_streak_update is 'automatically updates user streak data when a new journal entry is created (v2: fixed date arithmetic and added error handling)';

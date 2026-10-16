export const menuItemName = (name: string, generalName?: string | null): string => {
  const thematic = name.trim();
  const general = generalName?.trim() ?? '';
  if (!thematic) return general;
  return !general || thematic.toLocaleLowerCase() === general.toLocaleLowerCase()
    ? thematic
    : `${thematic} - ${general}`;
};

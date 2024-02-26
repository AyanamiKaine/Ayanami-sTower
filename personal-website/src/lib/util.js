export function slugify(text) {
    return text
      .toString()               
      .toLowerCase()            
      .replace(/\s+/g, '-')     // Replace spaces with -
      .replace(/[^\w\-]+/g, '') // Remove non-word chars
      .replace(/\-\-+/g, '-')   // Replace multiple - with single -
      .replace(/^-+/, '')       // Trim - from start
      .replace(/-+$/, '');      // Trim - from end
  }